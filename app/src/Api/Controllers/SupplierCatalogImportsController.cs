using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.SupplierCatalog;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Services;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/supplier-catalog-imports")]
public sealed class SupplierCatalogImportsController(
    ApplicationDbContext dbContext,
    SupplierCatalogCsvParser csvParser,
    SupplierCatalogMatcher matcher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SupplierCatalogImportListItemResponse>>> GetBatches(CancellationToken cancellationToken)
    {
        var batches = await dbContext.SupplierCatalogImportBatches
            .AsNoTracking()
            .Select(x => new SupplierCatalogImportListItemResponse(
                x.Id,
                x.SupplierName,
                x.FileName,
                x.Status,
                x.CreatedAt,
                x.Details.Count,
                x.Details.Count(d => d.RowStatus == "ready"),
                x.Details.Count(d => d.RowStatus == "warning"),
                x.Details.Count(d => d.RowStatus == "conflict" || d.RowStatus == "invalid"),
                x.Details.Count(d => d.AppliedAt != null)))
            .ToListAsync(cancellationToken);

        return Ok(batches.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("{batchId:guid}")]
    public async Task<ActionResult<SupplierCatalogImportPreviewResponse>> GetBatch(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.SupplierCatalogImportBatches
            .AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);

        if (batch is null)
        {
            return NotFound();
        }

        return Ok(ToPreviewResponse(batch));
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] SupplierCatalogImportPreviewRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        var parseResult = csvParser.Parse(request.SupplierName, request.CsvContent);
        if (!parseResult.IsSuccess)
        {
            return BadRequest(new { message = "CSV inválido", errors = parseResult.Errors });
        }

        var details = parseResult.Rows.ToList();
        await matcher.MatchAsync(details, cancellationToken);

        var batch = new SupplierCatalogImportBatch
        {
            SupplierName = request.SupplierName.Trim(),
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? "catalogo_proveedor.csv" : request.FileName.Trim(),
            UserId = userId,
            ConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            Status = details.Any(x => x.RowStatus == "conflict" || x.RowStatus == "invalid") ? "preview_with_conflicts" : "preview_ready",
            Details = details,
            SummaryJson = JsonSerializer.Serialize(BuildSummary(details))
        };

        await dbContext.SupplierCatalogImportBatches.AddAsync(batch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToPreviewResponse(batch));
    }

    [HttpPost("{batchId:guid}/apply")]
    public async Task<IActionResult> Apply(Guid batchId, [FromBody] SupplierCatalogImportApplyRequest request, CancellationToken cancellationToken)
    {
        var batch = await dbContext.SupplierCatalogImportBatches
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);

        if (batch is null)
        {
            return NotFound();
        }

        if (!string.Equals(batch.ConfirmationToken, request.ConfirmationToken, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Token de confirmación inválido" });
        }

        if (batch.Status == "applied")
        {
            return Conflict(new { message = "El lote ya fue aplicado" });
        }

        var actionableRows = batch.Details.Where(x => x.ApplySelected && x.ActionType is "update" or "create").OrderBy(x => x.SourceRow).ToList();
        if (actionableRows.Count == 0)
        {
            return Conflict(new { message = "El lote no contiene filas aplicables" });
        }

        var updatedProducts = 0;
        var createdProducts = 0;
        var conflictRows = batch.Details.Count(x => x.RowStatus is "conflict" or "invalid");
        var skippedRows = batch.Details.Count - actionableRows.Count;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var detail in actionableRows)
        {
            if (detail.ActionType == "update" && detail.MatchedProductId.HasValue)
            {
                var product = await dbContext.Products.SingleAsync(x => x.Id == detail.MatchedProductId.Value, cancellationToken);
                product.CurrentCost = detail.ProposedCost ?? product.CurrentCost;
                product.CurrentSalePrice = detail.ProposedSalePrice ?? product.CurrentSalePrice;
                product.UpdatedAt = DateTimeOffset.UtcNow;
                updatedProducts++;
            }
            else if (detail.ActionType == "create")
            {
                var product = new Product
                {
                    InternalKey = $"SUP-{batch.SupplierName[..Math.Min(batch.SupplierName.Length, 12)].ToUpperInvariant()}-{detail.SourceRow:D4}",
                    PrimaryCode = detail.SupplierProductCode,
                    Description = detail.Description,
                    Brand = detail.Brand,
                    CurrentCost = detail.ProposedCost,
                    CurrentSalePrice = detail.ProposedSalePrice,
                    Unit = detail.Unit,
                    RequiresReview = true,
                    ReviewReason = $"Alta desde catálogo proveedor {batch.SupplierName}",
                    Status = "activo"
                };

                await dbContext.Products.AddAsync(product, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                detail.MatchedProductId = product.Id;
                createdProducts++;
            }

            detail.AppliedAt = DateTimeOffset.UtcNow;
        }

        batch.Status = conflictRows == 0 ? "applied" : "applied_with_pending_conflicts";
        batch.AppliedAt = DateTimeOffset.UtcNow;
        batch.SummaryJson = JsonSerializer.Serialize(new
        {
            summary = BuildSummary(batch.Details),
            updatedProducts,
            createdProducts,
            skippedRows,
            conflictRows,
            appliedAt = batch.AppliedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new SupplierCatalogImportApplyResponse(batch.Id, batch.Status, updatedProducts, createdProducts, skippedRows, conflictRows));
    }

    private static object BuildSummary(IReadOnlyCollection<SupplierCatalogImportDetail> details)
    {
        return new
        {
            totalRows = details.Count,
            readyRows = details.Count(x => x.RowStatus == "ready"),
            warningRows = details.Count(x => x.RowStatus == "warning"),
            conflictRows = details.Count(x => x.RowStatus == "conflict" || x.RowStatus == "invalid"),
            newProducts = details.Count(x => x.MatchType == "new_product"),
            matchedProducts = details.Count(x => x.MatchedProductId != null)
        };
    }

    private static SupplierCatalogImportPreviewResponse ToPreviewResponse(SupplierCatalogImportBatch batch)
    {
        return new SupplierCatalogImportPreviewResponse(
            batch.Id,
            batch.SupplierName,
            batch.Status,
            batch.ConfirmationToken,
            batch.Details.Count,
            batch.Details.Count(x => x.RowStatus == "ready"),
            batch.Details.Count(x => x.RowStatus == "warning"),
            batch.Details.Count(x => x.RowStatus == "conflict" || x.RowStatus == "invalid"),
            batch.Details.Count(x => x.MatchType == "new_product"),
            batch.Details.Count(x => x.MatchedProductId != null),
            batch.Details.OrderBy(x => x.SourceRow)
                .Select(x => new SupplierCatalogImportRowResponse(
                    x.SourceRow,
                    x.SupplierProductCode,
                    x.Description,
                    x.Brand,
                    x.Cost,
                    x.SuggestedSalePrice,
                    x.MatchType,
                    x.ActionType,
                    x.RowStatus,
                    x.MatchedProductId,
                    x.ProposedCost,
                    x.ProposedSalePrice,
                    x.ApplySelected,
                    x.ReviewReason))
                .ToList());
    }
}
