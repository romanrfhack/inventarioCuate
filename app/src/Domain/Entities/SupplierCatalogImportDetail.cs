using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class SupplierCatalogImportDetail : Entity
{
    public Guid SupplierCatalogImportBatchId { get; set; }
    public int SourceRow { get; set; }
    public string? SupplierProductCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal? Cost { get; set; }
    public decimal? SuggestedSalePrice { get; set; }
    public string? Unit { get; set; }
    public string MatchType { get; set; } = "pending";
    public string ActionType { get; set; } = "review";
    public Guid? MatchedProductId { get; set; }
    public string RowStatus { get; set; } = "pending";
    public string? ReviewReason { get; set; }
    public decimal? ProposedCost { get; set; }
    public decimal? ProposedSalePrice { get; set; }
    public bool ApplySelected { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }

    public SupplierCatalogImportBatch SupplierCatalogImportBatch { get; set; } = null!;
}
