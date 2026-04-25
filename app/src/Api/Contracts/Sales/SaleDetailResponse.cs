namespace RefaccionariaCuate.Api.Contracts.Sales;

public sealed record SaleDetailResponse(
    Guid SaleId,
    string Folio,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    int ItemCount,
    decimal TotalQuantity,
    IReadOnlyCollection<SaleDetailItemResponse> Items);

public sealed record SaleDetailItemResponse(
    Guid ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
