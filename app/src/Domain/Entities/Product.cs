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
    public bool RequiresReview { get; set; }
    public string? ReviewReason { get; set; }
    public string Status { get; set; } = "activo";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public InventoryBalance? InventoryBalance { get; set; }
    public List<InventoryMovement> Movements { get; set; } = new();
}
