namespace RefaccionariaCuate.Application.Features.Inventory;

public sealed record InventorySummaryDto(Guid ProductId, string Description, decimal CurrentStock, DateTimeOffset UpdatedAt);
