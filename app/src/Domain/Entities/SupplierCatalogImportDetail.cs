using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class SupplierCatalogImportDetail : Entity
{
    public Guid SupplierCatalogImportBatchId { get; set; }
    public int SourceRow { get; set; }
    public string SourceSheet { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ImportProfile { get; set; } = string.Empty;
    public string? SupplierProductCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public decimal? PiecesPerBox { get; set; }
    public string? Compatibility { get; set; }
    public string? Line { get; set; }
    public string? Family { get; set; }
    public string? SubFamily { get; set; }
    public string? Category { get; set; }
    public decimal? Cost { get; set; }
    public decimal? SuggestedSalePrice { get; set; }
    public string? PriceLevelsJson { get; set; }
    public decimal? SupplierAvailability { get; set; }
    public string? SupplierStockText { get; set; }
    public bool RequiresRevision { get; set; }
    public string? RevisionReason { get; set; }
    public string MatchType { get; set; } = "pendiente";
    public string ActionType { get; set; } = "review";
    public string RowStatus { get; set; } = "pendiente";
    public Guid? MatchedProductId { get; set; }
    public string? ReviewReason { get; set; }
    public decimal? ProposedCost { get; set; }
    public decimal? ProposedSalePrice { get; set; }
    public bool ApplySelected { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }

    public SupplierCatalogImportBatch SupplierCatalogImportBatch { get; set; } = null!;
}
