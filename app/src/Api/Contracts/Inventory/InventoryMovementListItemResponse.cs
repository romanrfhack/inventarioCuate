namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed record InventoryMovementListItemResponse(
    Guid MovementId,
    Guid ProductId,
    string Description,
    string MovementType,
    decimal Quantity,
    decimal? ResultingStock,
    string? Reason,
    string SourceType,
    string? SourceId,
    DateTimeOffset CreatedAt);
