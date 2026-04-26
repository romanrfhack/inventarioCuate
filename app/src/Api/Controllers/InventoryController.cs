using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Inventory;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Domain.Enums;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyCollection<InventorySummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var response = await dbContext.InventoryBalances
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new InventorySummaryResponse(x.ProductId, x.Product.Description, x.CurrentStock, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InventoryMovementListItemResponse>>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] string? movementType,
        [FromQuery] string? reason,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Product)
            .AsQueryable();

        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(movementType))
        {
            var normalizedType = movementType.Trim().ToLowerInvariant();
            query = query.Where(x => x.MovementType == normalizedType);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var normalizedReason = reason.Trim();
            query = query.Where(x => x.Reason != null && EF.Functions.Like(x.Reason, $"%{normalizedReason}%"));
        }

        var movements = await query
            .ToListAsync(cancellationToken);

        movements = movements
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToList();

        if (dateFrom.HasValue)
        {
            movements = movements
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= dateFrom.Value)
                .ToList();
        }

        if (dateTo.HasValue)
        {
            movements = movements
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= dateTo.Value)
                .ToList();
        }

        return Ok(movements
            .Select(MapMovementListItem)
            .ToList());
    }

    [HttpGet("movements/{movementId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryMovementDetailResponse>> GetMovementDetail(Guid movementId, CancellationToken cancellationToken)
    {
        var movement = await dbContext.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == movementId, cancellationToken);

        if (movement is null)
        {
            return NotFound(new { code = "404_MOVEMENT_NOT_FOUND", message = "El movimiento no existe." });
        }

        return Ok(MapMovementDetail(movement));
    }

    [HttpPost("entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<InventoryMovementResponse>> RegisterEntry([FromBody] RegisterInventoryEntryRequest request, CancellationToken cancellationToken)
    {
        return RegisterMovement(
            request.ProductId,
            request.Quantity,
            request.Reason,
            MovementType.EntradaManual,
            "manual_entry",
            cancellationToken);
    }

    [HttpPost("adjustments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InventoryMovementResponse>> RegisterAdjustment([FromBody] RegisterInventoryAdjustmentRequest request, CancellationToken cancellationToken)
    {
        return RegisterMovement(
            request.ProductId,
            request.QuantityDelta,
            request.Reason,
            MovementType.AjusteManual,
            "manual_adjustment",
            cancellationToken);
    }

    private async Task<ActionResult<InventoryMovementResponse>> RegisterMovement(
        Guid productId,
        decimal quantity,
        string? reason,
        string movementType,
        string sourceType,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "El producto es obligatorio." });
        }

        if (quantity == 0)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "La cantidad debe ser distinta de cero." });
        }

        if (movementType == MovementType.EntradaManual && quantity < 0)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "La entrada manual debe ser mayor a cero." });
        }

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "El motivo es obligatorio." });
        }

        if (normalizedReason.Length > 200)
        {
            return BadRequest(new { code = "400_VALIDATION_ERROR", message = "El motivo no puede exceder 200 caracteres." });
        }

        var product = await dbContext.Products
            .Include(x => x.InventoryBalance)
            .SingleOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null)
        {
            return NotFound(new { code = "404_PRODUCT_NOT_FOUND", message = "El producto no existe." });
        }

        var now = DateTimeOffset.UtcNow;
        var balance = product.InventoryBalance;
        if (balance is null)
        {
            balance = new InventoryBalance
            {
                ProductId = product.Id,
                CurrentStock = 0,
                UpdatedAt = now
            };

            product.InventoryBalance = balance;
            await dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
        }

        var resultingStock = balance.CurrentStock + quantity;
        if (resultingStock < 0)
        {
            return Conflict(new
            {
                code = "409_NEGATIVE_STOCK_NOT_ALLOWED",
                message = $"El ajuste deja stock negativo para {product.Description}.",
                currentStock = balance.CurrentStock,
                requestedDelta = quantity
            });
        }

        balance.CurrentStock = resultingStock;
        balance.UpdatedAt = now;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            MovementType = movementType,
            Quantity = quantity,
            ResultingStock = resultingStock,
            SourceType = sourceType,
            SourceId = null,
            Reason = normalizedReason,
            UserId = TryGetUserId(),
            CreatedAt = now
        };

        await dbContext.InventoryMovements.AddAsync(movement, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new InventoryMovementResponse(
            movement.Id,
            product.Id,
            product.Description,
            movement.MovementType,
            movement.Quantity,
            resultingStock,
            normalizedReason,
            movement.CreatedAt));
    }

    private Guid? TryGetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private static InventoryMovementListItemResponse MapMovementListItem(InventoryMovement movement)
    {
        return new InventoryMovementListItemResponse(
            movement.Id,
            movement.ProductId,
            movement.Product.Description,
            movement.MovementType,
            movement.Quantity,
            movement.ResultingStock,
            movement.Reason,
            movement.SourceType,
            movement.SourceId,
            movement.CreatedAt);
    }

    private static InventoryMovementDetailResponse MapMovementDetail(InventoryMovement movement)
    {
        return new InventoryMovementDetailResponse(
            movement.Id,
            movement.ProductId,
            movement.Product.Description,
            movement.MovementType,
            movement.Quantity,
            movement.ResultingStock,
            movement.Reason,
            movement.SourceType,
            movement.SourceId,
            movement.UserId,
            movement.ShiftId,
            movement.CreatedAt);
    }
}
