using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class InventoryMovement : Entity
{
    public Guid ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? ResultingStock { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public string? Reason { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product Product { get; set; } = null!;
}
