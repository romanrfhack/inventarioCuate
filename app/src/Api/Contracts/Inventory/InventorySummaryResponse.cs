namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed record InventorySummaryResponse(Guid ProductId, string Description, decimal CurrentStock, DateTimeOffset UpdatedAt);
