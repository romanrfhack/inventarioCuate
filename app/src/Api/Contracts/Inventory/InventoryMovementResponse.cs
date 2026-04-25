namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed record InventoryMovementResponse(
    Guid MovementId,
    Guid ProductId,
    string Description,
    string MovementType,
    decimal Quantity,
    decimal ResultingStock,
    string Reason,
    DateTimeOffset CreatedAt);
