using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class Product : Entity
{
    public string InternalKey { get; set; } = string.Empty;
    public string? PrimaryCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal? CurrentCost { get; set; }
    public decimal? CurrentSalePrice { get; set; }
    public string? Unit { get; set; }
    public decimal? PiecesPerBox { get; set; }
    public string? Compatibility { get; set; }
    public string? Line { get; set; }
    public string? Family { get; set; }
    public string? SubFamily { get; set; }
    public string? Category { get; set; }
    public string? SupplierName { get; set; }
    public decimal? SupplierAvailability { get; set; }
    public string? SupplierStockText { get; set; }
    public DateTimeOffset? SupplierCatalogUpdatedAt { get; set; }
    public bool RequiresReview { get; set; }
    public string? ReviewReason { get; set; }
    public string Status { get; set; } = "activo";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public InventoryBalance? InventoryBalance { get; set; }
    public List<InventoryMovement> Movements { get; set; } = new();
    public List<SaleDetail> SaleDetails { get; set; } = new();
}
