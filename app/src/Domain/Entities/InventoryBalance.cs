using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class InventoryBalance : Entity
{
    public Guid ProductId { get; set; }
    public decimal CurrentStock { get; set; }
    public string? Location { get; set; }
    public DateOnly? BaseCutDate { get; set; }
    public string? BaseOrigin { get; set; }
    public bool RequiresReview { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product Product { get; set; } = null!;
}
