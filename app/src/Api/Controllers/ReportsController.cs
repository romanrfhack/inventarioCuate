using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Reports;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("operations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationsReportResponse>> GetOperationsReport(CancellationToken cancellationToken)
    {
        var inventoryRows = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.InventoryBalance)
            .OrderBy(x => x.Description)
            .Select(x => new
            {
                x.Id,
                x.InternalKey,
                x.PrimaryCode,
                x.Description,
                x.Brand,
                x.CurrentCost,
                x.CurrentSalePrice,
                x.RequiresReview,
                x.ReviewReason,
                CurrentStock = x.InventoryBalance != null ? x.InventoryBalance.CurrentStock : 0m,
                UpdatedAt = x.InventoryBalance != null ? x.InventoryBalance.UpdatedAt : x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var recentSales = (await dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Details)
                .ThenInclude(x => x.Product)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAt)
            .Take(15)
            .ToList();

        var inventory = inventoryRows
            .Select(row =>
            {
                var flags = BuildFlags(row.PrimaryCode, row.CurrentCost, row.CurrentSalePrice, row.RequiresReview, row.ReviewReason, row.CurrentStock);
                var estimatedCostValue = row.CurrentCost.HasValue ? decimal.Round(row.CurrentCost.Value * row.CurrentStock, 2) : 0m;
                var estimatedRetailValue = row.CurrentSalePrice.HasValue ? decimal.Round(row.CurrentSalePrice.Value * row.CurrentStock, 2) : 0m;

                return new OperationsInventoryItemResponse(
                    row.Id,
                    row.InternalKey,
                    row.PrimaryCode,
                    row.Description,
                    row.Brand,
                    row.CurrentStock,
                    row.CurrentCost,
                    row.CurrentSalePrice,
                    estimatedCostValue,
                    estimatedRetailValue,
                    row.RequiresReview,
                    flags,
                    row.UpdatedAt);
            })
            .OrderByDescending(x => x.Flags.Count)
            .ThenBy(x => x.CurrentStock)
            .ThenBy(x => x.Description)
            .ToList();

        var confirmedSales = recentSales.Where(x => x.Status == "confirmed").ToList();
        var recentSalesResponse = recentSales
            .Select(sale =>
            {
                decimal? grossProfit = null;
                if (sale.Details.All(detail => detail.Product.CurrentCost.HasValue))
                {
                    grossProfit = decimal.Round(sale.Details.Sum(detail => (detail.UnitPrice - detail.Product.CurrentCost!.Value) * detail.Quantity), 2);
                }

                return new OperationsRecentSaleResponse(
                    sale.Id,
                    sale.Folio,
                    sale.Status,
                    sale.Total,
                    sale.Details.Sum(detail => detail.Quantity),
                    sale.Details.Count,
                    grossProfit,
                    sale.CreatedAt);
            })
            .ToList();

        var productAnomalies = inventory
            .Where(x => x.Flags.Count > 0)
            .Select(x => new OperationsProductAnomalyResponse(
                x.ProductId,
                x.InternalKey,
                x.Description,
                x.CurrentStock,
                x.Flags.Contains("stock_negativo") || x.Flags.Contains("sin_precio") || x.Flags.Contains("sin_costo"),
                x.Flags))
            .ToList();

        var profitableProductBase = confirmedSales
            .SelectMany(sale => sale.Details
                .Where(detail => detail.Product.CurrentCost.HasValue)
                .Select(detail => new
                {
                    detail.ProductId,
                    detail.Product.InternalKey,
                    detail.Product.Description,
                    detail.Quantity,
                    detail.LineTotal,
                    GrossProfit = decimal.Round((detail.UnitPrice - detail.Product.CurrentCost!.Value) * detail.Quantity, 2)
                }))
            .GroupBy(x => new { x.ProductId, x.InternalKey, x.Description })
            .Select(group => new OperationsProfitableProductResponse(
                group.Key.ProductId,
                group.Key.InternalKey,
                group.Key.Description,
                group.Sum(x => x.Quantity),
                decimal.Round(group.Sum(x => x.LineTotal), 2),
                decimal.Round(group.Sum(x => x.GrossProfit), 2),
                group.Count()))
            .OrderByDescending(x => x.GrossProfit)
            .ThenByDescending(x => x.SalesAmount)
            .ToList();

        var profitableProducts = profitableProductBase
            .Take(5)
            .ToList();

        var summary = new OperationsReportSummaryResponse(
            inventory.Count,
            inventory.Count(x => x.CurrentStock > 0),
            inventory.Count(x => x.CurrentStock == 0),
            inventory.Count(x => x.CurrentStock < 0),
            inventory.Sum(x => x.CurrentStock),
            decimal.Round(inventory.Sum(x => x.EstimatedCostValue), 2),
            decimal.Round(inventory.Sum(x => x.EstimatedRetailValue), 2),
            confirmedSales.Count,
            decimal.Round(confirmedSales.Sum(x => x.Total), 2),
            decimal.Round(profitableProductBase.Sum(x => x.GrossProfit), 2),
            confirmedSales.Count > 0 ? DateOnly.FromDateTime(confirmedSales.Max(x => x.CreatedAt.UtcDateTime)) : null);

        return Ok(new OperationsReportResponse(summary, inventory, recentSalesResponse, productAnomalies, profitableProducts));
    }

    private static List<string> BuildFlags(string? primaryCode, decimal? currentCost, decimal? currentSalePrice, bool requiresReview, string? reviewReason, decimal currentStock)
    {
        var flags = new List<string>();

        if (string.IsNullOrWhiteSpace(primaryCode))
        {
            flags.Add("sin_codigo");
        }

        if (!currentCost.HasValue || currentCost.Value <= 0)
        {
            flags.Add("sin_costo");
        }

        if (!currentSalePrice.HasValue || currentSalePrice.Value <= 0)
        {
            flags.Add("sin_precio");
        }

        if (requiresReview)
        {
            flags.Add(string.IsNullOrWhiteSpace(reviewReason) ? "requiere_revision" : $"requiere_revision:{reviewReason}");
        }

        if (currentStock < 0)
        {
            flags.Add("stock_negativo");
        }
        else if (currentStock == 0)
        {
            flags.Add("sin_existencia");
        }

        return flags;
    }
}
