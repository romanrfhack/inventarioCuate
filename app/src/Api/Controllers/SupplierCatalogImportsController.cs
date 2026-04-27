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
[Route("api/provider-catalogs")]
public sealed class SupplierCatalogImportsController(
    ApplicationDbContext dbContext,
    SupplierCatalogSpreadsheetParser parser,
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
                x.ImportProfile,
                x.FileName,
                x.Status,
                x.CreatedAt,
                x.Details.Count,
                x.Details.Count(d => d.RowStatus == "match_codigo"),
                x.Details.Count(d => d.RowStatus == "producto_nuevo"),
                x.Details.Count(d => d.RowStatus == "dato_incompleto"),
                x.Details.Count(d => d.RowStatus == "requiere_revision"),
                x.Details.Count(d => d.AppliedAt != null)))
            .ToListAsync(cancellationToken);

        return Ok(batches.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("profiles")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyCollection<SupplierCatalogSpreadsheetParser.ImportProfileDescriptor>> GetProfiles()
    {
        return Ok(parser.GetProfiles());
    }

    [HttpGet("{batchId:guid}")]
    public async Task<ActionResult<SupplierCatalogImportPreviewResponse>> GetBatch(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.SupplierCatalogImportBatches
            .AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);

        return batch is null ? NotFound() : Ok(ToPreviewResponse(batch));
    }

    [HttpPost("preview")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Preview([FromForm] SupplierCatalogImportPreviewRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "El archivo es obligatorio" });
        }

        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        await using var stream = request.File.OpenReadStream();
        var parseResult = parser.Parse(request.SupplierName, request.ImportProfile, request.File.FileName, stream);
        if (!parseResult.IsSuccess)
        {
            return BadRequest(new { message = "Archivo inválido", errors = parseResult.Errors });
        }

        var details = parseResult.Rows.ToList();
        await matcher.MatchAsync(details, cancellationToken);

        var batch = new SupplierCatalogImportBatch
        {
            SupplierName = request.SupplierName.Trim(),
            ImportProfile = request.ImportProfile.Trim(),
            FileName = request.File.FileName,
            UserId = userId,
            ConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            Status = details.Any(x => x.RowStatus is "dato_incompleto" or "requiere_revision") ? "preview_con_revision" : "preview_lista",
            Details = details,
            SummaryJson = JsonSerializer.Serialize(BuildSummary(details))
        };

        await dbContext.SupplierCatalogImportBatches.AddAsync(batch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToPreviewResponse(batch));
    }

    [HttpPost("apply/{batchId:guid}")]
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

        if (batch.Status == "applied" || batch.Status == "applied_with_pending_review")
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
        var reviewRows = batch.Details.Count(x => x.RowStatus is "dato_incompleto" or "requiere_revision");
        var skippedRows = batch.Details.Count - actionableRows.Count;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var detail in actionableRows)
        {
            Product? product = null;

            if (detail.ActionType == "update" && detail.MatchedProductId.HasValue)
            {
                product = await dbContext.Products
                    .Include(x => x.InventoryBalance)
                    .SingleAsync(x => x.Id == detail.MatchedProductId.Value, cancellationToken);

                ApplyCatalogFields(product, detail);
                updatedProducts++;
            }
            else if (detail.ActionType == "create")
            {
                product = new Product
                {
                    InternalKey = $"SUP-{batch.ImportProfile.ToUpperInvariant()}-{detail.SourceRow:D5}",
                    PrimaryCode = detail.SupplierProductCode,
                    Description = detail.Description,
                    Status = "activo",
                    RequiresReview = false
                };

                ApplyCatalogFields(product, detail);
                await dbContext.Products.AddAsync(product, cancellationToken);
                createdProducts++;
            }

            if (product is not null)
            {
                await UpsertSupplierSnapshotAsync(product, batch, detail, cancellationToken);
                detail.MatchedProductId = product.Id;
            }

            detail.AppliedAt = DateTimeOffset.UtcNow;
        }

        batch.Status = reviewRows == 0 ? "applied" : "applied_with_pending_review";
        batch.AppliedAt = DateTimeOffset.UtcNow;
        batch.SummaryJson = JsonSerializer.Serialize(new
        {
            summary = BuildSummary(batch.Details),
            updatedProducts,
            createdProducts,
            skippedRows,
            reviewRows,
            appliedAt = batch.AppliedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new SupplierCatalogImportApplyResponse(batch.Id, batch.Status, updatedProducts, createdProducts, skippedRows, reviewRows));
    }

    private async Task UpsertSupplierSnapshotAsync(Product product, SupplierCatalogImportBatch batch, SupplierCatalogImportDetail detail, CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.ProductSupplierCatalogSnapshots
            .SingleOrDefaultAsync(
                x => x.ProductId == product.Id && x.SupplierProfile == batch.ImportProfile,
                cancellationToken);

        var isNewSnapshot = snapshot is null;
        snapshot ??= new ProductSupplierCatalogSnapshot
        {
            ProductId = product.Id,
            SupplierProfile = batch.ImportProfile
        };

        snapshot.SupplierName = detail.SupplierName;
        snapshot.SupplierCode = detail.SupplierProductCode;
        snapshot.SupplierDescription = detail.Description;
        snapshot.SupplierBrand = detail.Brand;
        snapshot.SupplierCost = detail.Cost;
        snapshot.SuggestedSalePrice = detail.SuggestedSalePrice;
        snapshot.PriceLevelsJson = detail.PriceLevelsJson;
        snapshot.SupplierAvailability = detail.SupplierAvailability;
        snapshot.SupplierStockText = detail.SupplierStockText;
        snapshot.Compatibility = detail.Compatibility;
        snapshot.Category = detail.Category;
        snapshot.Line = detail.Line;
        snapshot.Family = detail.Family;
        snapshot.SubFamily = detail.SubFamily;
        snapshot.LastImportBatchId = batch.Id;
        snapshot.LastImportedAt = DateTimeOffset.UtcNow;
        snapshot.RequiresReview = detail.RequiresRevision;
        snapshot.ReviewReason = detail.ReviewReason;

        if (isNewSnapshot)
        {
            await dbContext.ProductSupplierCatalogSnapshots.AddAsync(snapshot, cancellationToken);
        }
    }

    private static void ApplyCatalogFields(Product product, SupplierCatalogImportDetail detail)
    {
        product.PrimaryCode ??= detail.SupplierProductCode;
        product.Description = string.IsNullOrWhiteSpace(product.Description) ? detail.Description : product.Description;
        product.Brand ??= detail.Brand;
        product.Unit ??= detail.Unit;
        product.PiecesPerBox ??= detail.PiecesPerBox;
        product.Compatibility ??= detail.Compatibility;
        product.Line ??= detail.Line;
        product.Family ??= detail.Family;
        product.SubFamily ??= detail.SubFamily;
        product.Category ??= detail.Category;
        product.CurrentCost = detail.ProposedCost ?? product.CurrentCost;
        product.CurrentSalePrice = detail.ProposedSalePrice ?? product.CurrentSalePrice;
        product.SupplierCatalogUpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static object BuildSummary(IReadOnlyCollection<SupplierCatalogImportDetail> details)
    {
        return new
        {
            totalRows = details.Count,
            matchCodigoRows = details.Count(x => x.RowStatus == "match_codigo"),
            productoNuevoRows = details.Count(x => x.RowStatus == "producto_nuevo"),
            datoIncompletoRows = details.Count(x => x.RowStatus == "dato_incompleto"),
            requiereRevisionRows = details.Count(x => x.RowStatus == "requiere_revision"),
            appliedRows = details.Count(x => x.AppliedAt != null)
        };
    }

    private static SupplierCatalogImportPreviewResponse ToPreviewResponse(SupplierCatalogImportBatch batch)
    {
        return new SupplierCatalogImportPreviewResponse(
            batch.Id,
            batch.SupplierName,
            batch.ImportProfile,
            batch.FileName,
            batch.Status,
            batch.ConfirmationToken,
            batch.Details.Count,
            batch.Details.Count(x => x.RowStatus == "match_codigo"),
            batch.Details.Count(x => x.RowStatus == "producto_nuevo"),
            batch.Details.Count(x => x.RowStatus == "dato_incompleto"),
            batch.Details.Count(x => x.RowStatus == "requiere_revision"),
            batch.Details.Count(x => x.AppliedAt != null),
            batch.Details.OrderBy(x => x.SourceRow)
                .Select(x => new SupplierCatalogImportRowResponse(
                    x.SourceRow,
                    x.SourceSheet,
                    x.SupplierProductCode,
                    x.Description,
                    x.Brand,
                    x.Unit,
                    x.PiecesPerBox,
                    x.Compatibility,
                    x.Line,
                    x.Family,
                    x.SubFamily,
                    x.Category,
                    x.Cost,
                    x.SuggestedSalePrice,
                    x.PriceLevelsJson,
                    x.SupplierAvailability,
                    x.SupplierStockText,
                    x.RequiresRevision,
                    x.RevisionReason,
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
