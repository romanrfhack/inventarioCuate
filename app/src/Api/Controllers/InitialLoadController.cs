using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.InitialLoad;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Services;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/initial-load")]
public sealed class InitialLoadController(ApplicationDbContext dbContext, InitialInventoryCsvParser csvParser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InitialLoadListItemResponse>>> GetLoads(CancellationToken cancellationToken)
    {
        var loads = (await dbContext.InitialInventoryLoads
            .AsNoTracking()
            .Select(x => new InitialLoadListItemResponse(
                x.Id,
                x.FileName ?? string.Empty,
                x.Status,
                x.LoadType,
                x.CreatedAt,
                x.Details.Count,
                x.Details.Count(d => d.RowStatus == "valid"),
                x.Details.Count(d => d.RowStatus == "invalid"),
                x.Details.Count(d => d.RowStatus == "warning")))
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Ok(loads);
    }

    [HttpGet("{loadId:guid}")]
    public async Task<IActionResult> GetLoad(Guid loadId, CancellationToken cancellationToken)
    {
        var load = await dbContext.InitialInventoryLoads
            .AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == loadId, cancellationToken);

        if (load is null)
        {
            return NotFound();
        }

        var summary = JsonDocument.Parse(load.SummaryJson).RootElement;
        return Ok(new
        {
            load.Id,
            load.FileName,
            load.Status,
            load.LoadType,
            load.CreatedAt,
            summary,
            rows = load.Details
                .OrderBy(x => x.SourceRow)
                .Select(x => new InitialLoadPreviewRowResponse(x.SourceRow, x.Code, x.Description, x.InitialStock, x.RowStatus, x.ReviewReason))
                .ToList()
        });
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] InitialLoadPreviewRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        var parseResult = csvParser.Parse(request.CsvContent);
        if (!parseResult.IsSuccess)
        {
            return BadRequest(new { message = "CSV inválido", errors = parseResult.Errors });
        }

        var confirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var totalRows = parseResult.Rows.Count;
        var validRows = parseResult.Rows.Count(x => x.RowStatus == "valid");
        var invalidRows = parseResult.Rows.Count(x => x.RowStatus == "invalid");
        var warningRows = parseResult.Rows.Count(x => x.RowStatus == "warning");
        var status = invalidRows == 0 ? "previewed" : "preview_failed";

        var load = new InitialInventoryLoad
        {
            LoadType = "manual_csv",
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? "inventario_inicial.csv" : request.FileName,
            Status = status,
            UserId = userId,
            ConfirmationToken = confirmationToken,
            SummaryJson = JsonSerializer.Serialize(new
            {
                rows = totalRows,
                valid = validRows,
                invalid = invalidRows,
                warnings = warningRows
            }),
            Details = parseResult.Rows.ToList()
        };

        await dbContext.InitialInventoryLoads.AddAsync(load, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new InitialLoadPreviewSummaryResponse(
            load.Id,
            load.Status,
            load.ConfirmationToken,
            totalRows,
            validRows,
            invalidRows,
            warningRows,
            load.Details
                .OrderBy(x => x.SourceRow)
                .Select(x => new InitialLoadPreviewRowResponse(x.SourceRow, x.Code, x.Description, x.InitialStock, x.RowStatus, x.ReviewReason))
                .ToList());

        return Ok(response);
    }

    [HttpPost("apply/{loadId:guid}")]
    public async Task<IActionResult> Apply(Guid loadId, [FromBody] InitialLoadApplyRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        var load = await dbContext.InitialInventoryLoads
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == loadId, cancellationToken);

        if (load is null)
        {
            return NotFound();
        }

        if (!string.Equals(load.ConfirmationToken, request.ConfirmationToken, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Token de confirmación inválido" });
        }

        if (!string.Equals(load.Status, "previewed", StringComparison.Ordinal))
        {
            return Conflict(new { message = "La carga no está en estado válido para apply" });
        }

        if (load.Details.Any(x => x.RowStatus == "invalid"))
        {
            return Conflict(new { message = "La carga contiene filas inválidas y no puede aplicarse" });
        }

        var createdProducts = 0;
        var matchedProducts = 0;
        var createdBalances = 0;
        var createdMovements = 0;
        var warningRows = load.Details.Count(x => x.RowStatus == "warning");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var detail in load.Details.OrderBy(x => x.SourceRow))
        {
            Product? product = null;
            if (!string.IsNullOrWhiteSpace(detail.Code))
            {
                product = await dbContext.Products
                    .Include(x => x.InventoryBalance)
                    .SingleOrDefaultAsync(x => x.PrimaryCode == detail.Code, cancellationToken);
            }

            if (product is null)
            {
                product = new Product
                {
                    InternalKey = $"LOAD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{detail.SourceRow}",
                    PrimaryCode = detail.Code,
                    Description = detail.Description,
                    Brand = detail.Brand,
                    CurrentCost = detail.Cost,
                    CurrentSalePrice = detail.SalePrice,
                    Unit = detail.Unit,
                    RequiresReview = string.IsNullOrWhiteSpace(detail.Code) || !detail.Cost.HasValue || !detail.SalePrice.HasValue || string.IsNullOrWhiteSpace(detail.Supplier),
                    ReviewReason = detail.ReviewReason,
                    Status = "activo"
                };

                await dbContext.Products.AddAsync(product, cancellationToken);
                createdProducts++;
            }
            else
            {
                matchedProducts++;
                if (string.IsNullOrWhiteSpace(product.Brand) && !string.IsNullOrWhiteSpace(detail.Brand)) product.Brand = detail.Brand;
                if (!product.CurrentCost.HasValue && detail.Cost.HasValue) product.CurrentCost = detail.Cost;
                if (!product.CurrentSalePrice.HasValue && detail.SalePrice.HasValue) product.CurrentSalePrice = detail.SalePrice;
                if (string.IsNullOrWhiteSpace(product.Unit) && !string.IsNullOrWhiteSpace(detail.Unit)) product.Unit = detail.Unit;
                product.RequiresReview = product.RequiresReview || detail.RowStatus == "warning";
                product.ReviewReason = string.Join(";", new[] { product.ReviewReason, detail.ReviewReason }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            detail.MatchedProductId = product.Id;

            var balance = await dbContext.InventoryBalances.SingleOrDefaultAsync(x => x.ProductId == product.Id, cancellationToken);
            if (balance is null)
            {
                balance = new InventoryBalance
                {
                    ProductId = product.Id,
                    CurrentStock = detail.InitialStock,
                    Location = detail.Location,
                    BaseOrigin = $"initial_load:{load.Id}",
                    RequiresReview = detail.RowStatus == "warning"
                };
                await dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
                createdBalances++;
            }
            else
            {
                balance.CurrentStock = detail.InitialStock;
                balance.Location = string.IsNullOrWhiteSpace(balance.Location) ? detail.Location : balance.Location;
                balance.BaseOrigin = $"initial_load:{load.Id}";
                balance.RequiresReview = balance.RequiresReview || detail.RowStatus == "warning";
                balance.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.InventoryMovements.AddAsync(new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = "carga_inicial",
                Quantity = detail.InitialStock,
                ResultingStock = detail.InitialStock,
                SourceType = "initial_load",
                SourceId = load.Id.ToString(),
                Reason = $"Carga inicial fila {detail.SourceRow}",
                UserId = userId
            }, cancellationToken);
            createdMovements++;
        }

        load.Status = "applied";
        load.SummaryJson = JsonSerializer.Serialize(new
        {
            rows = load.Details.Count,
            createdProducts,
            matchedProducts,
            createdBalances,
            createdMovements,
            warningRows,
            appliedAt = DateTimeOffset.UtcNow,
            appliedBy = userId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Accepted(new InitialLoadApplyResponse(load.Id, load.Status, createdProducts, matchedProducts, createdBalances, createdMovements, warningRows));
    }
}
