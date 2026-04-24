using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class InitialInventoryLoad : Entity
{
    public string LoadType { get; set; } = "manual_csv";
    public string? FileName { get; set; }
    public string Status { get; set; } = "pending";
    public string SummaryJson { get; set; } = "{}";
    public Guid UserId { get; set; }
    public string ConfirmationToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<InitialInventoryLoadDetail> Details { get; set; } = new();
}
