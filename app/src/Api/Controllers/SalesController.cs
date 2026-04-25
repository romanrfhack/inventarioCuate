using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Sales;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Domain.Enums;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public sealed class SalesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("quick")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuickSaleResponse>> CreateQuickSale([FromBody] CreateQuickSaleRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "Debe capturarse al menos un producto." });
        }

        if (request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "Cada partida requiere producto y cantidad mayor a cero." });
        }

        var distinctProductIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Include(x => x.InventoryBalance)
            .Where(x => distinctProductIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != distinctProductIds.Count)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "Uno o más productos no existen." });
        }

        var normalizedItems = new List<(Product Product, decimal Quantity, decimal UnitPrice, decimal LineTotal)>();
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var unitPrice = item.UnitPrice ?? product.CurrentSalePrice;
            if (!unitPrice.HasValue || unitPrice.Value <= 0)
            {
                return Conflict(new { code = "409_PRICE_REQUIRED", message = $"El producto {product.Description} no tiene precio de venta vigente." });
            }

            normalizedItems.Add((product, item.Quantity, unitPrice.Value, decimal.Round(item.Quantity * unitPrice.Value, 2)));
        }

        var requestedByProduct = normalizedItems
            .GroupBy(x => x.Product.Id)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));

        foreach (var pair in requestedByProduct)
        {
            var product = products[pair.Key];
            var balance = product.InventoryBalance;
            var currentStock = balance?.CurrentStock ?? 0m;
            if (balance is null || currentStock < pair.Value)
            {
                return Conflict(new
                {
                    code = "409_INSUFFICIENT_STOCK",
                    message = $"Stock insuficiente para {product.Description}.",
                    productId = product.Id,
                    availableStock = currentStock,
                    requestedQuantity = pair.Value
                });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var folioPrefix = $"VTA-{now:yyyyMMdd}";
        var dailyCount = await dbContext.Sales.CountAsync(x => x.Folio.StartsWith(folioPrefix), cancellationToken);
        var sale = new Sale
        {
            Folio = $"{folioPrefix}-{dailyCount + 1:0000}",
            Status = "confirmed",
            Total = normalizedItems.Sum(x => x.LineTotal),
            UserId = TryGetUserId(),
            CreatedAt = now
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Sales.AddAsync(sale, cancellationToken);

        var remainingByProduct = new Dictionary<Guid, decimal>();
        foreach (var item in normalizedItems)
        {
            var balance = item.Product.InventoryBalance!;
            balance.CurrentStock -= item.Quantity;
            balance.UpdatedAt = now;
            remainingByProduct[item.Product.Id] = balance.CurrentStock;

            sale.Details.Add(new SaleDetail
            {
                SaleId = sale.Id,
                ProductId = item.Product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            });

            await dbContext.InventoryMovements.AddAsync(new InventoryMovement
            {
                ProductId = item.Product.Id,
                MovementType = MovementType.Venta,
                Quantity = item.Quantity,
                ResultingStock = balance.CurrentStock,
                SourceType = "sale",
                SourceId = sale.Id.ToString(),
                Reason = $"Venta rápida {sale.Folio}",
                UserId = sale.UserId,
                CreatedAt = now
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new QuickSaleResponse(
            sale.Id,
            sale.Folio,
            sale.Total,
            sale.CreatedAt,
            normalizedItems.Select(x => new QuickSaleDetailResponse(
                x.Product.Id,
                x.Product.Description,
                x.Quantity,
                x.UnitPrice,
                x.LineTotal,
                remainingByProduct[x.Product.Id]))
            .ToList());

        return Ok(response);
    }

    private Guid? TryGetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
