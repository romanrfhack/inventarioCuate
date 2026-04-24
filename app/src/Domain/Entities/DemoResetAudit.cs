using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class DemoResetAudit : Entity
{
    public Guid ExecutedByUserId { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SummaryJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
