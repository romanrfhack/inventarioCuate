using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class SupplierCatalogImportBatch : Entity
{
    public string SupplierName { get; set; } = string.Empty;
    public string ImportProfile { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string SummaryJson { get; set; } = "{}";
    public Guid UserId { get; set; }
    public string ConfirmationToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; set; }

    public List<SupplierCatalogImportDetail> Details { get; set; } = new();
}
