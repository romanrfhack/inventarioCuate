namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed record InventoryMovementDetailResponse(
    Guid MovementId,
    Guid ProductId,
    string Description,
    string MovementType,
    decimal Quantity,
    decimal? ResultingStock,
    string? Reason,
    string SourceType,
    string? SourceId,
    Guid? UserId,
    Guid? ShiftId,
    DateTimeOffset CreatedAt);
