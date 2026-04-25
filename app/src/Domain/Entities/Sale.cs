using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class Sale : Entity
{
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = "confirmed";
    public decimal Total { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<SaleDetail> Details { get; set; } = new();
}
