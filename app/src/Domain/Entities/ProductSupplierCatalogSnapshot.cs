using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class ProductSupplierCatalogSnapshot : Entity
{
    public Guid ProductId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierProfile { get; set; } = string.Empty;
    public string? SupplierCode { get; set; }
    public string? SupplierDescription { get; set; }
    public string? SupplierBrand { get; set; }
    public decimal? SupplierCost { get; set; }
    public decimal? SuggestedSalePrice { get; set; }
    public string? PriceLevelsJson { get; set; }
    public decimal? SupplierAvailability { get; set; }
    public string? SupplierStockText { get; set; }
    public string? Compatibility { get; set; }
    public string? Category { get; set; }
    public string? Line { get; set; }
    public string? Family { get; set; }
    public string? SubFamily { get; set; }
    public Guid LastImportBatchId { get; set; }
    public DateTimeOffset LastImportedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool RequiresReview { get; set; }
    public string? ReviewReason { get; set; }

    public Product Product { get; set; } = null!;
}
