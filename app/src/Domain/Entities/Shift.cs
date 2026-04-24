using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class Shift : Entity
{
    public Guid UserId { get; set; }
    public string Status { get; set; } = "abierto";
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Notes { get; set; }
}
