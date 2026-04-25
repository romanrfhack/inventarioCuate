namespace RefaccionariaCuate.Api.Contracts.Sales;

public sealed record SaleListItemResponse(
    Guid SaleId,
    string Folio,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    int ItemCount,
    decimal TotalQuantity,
    IReadOnlyCollection<SaleListDetailResponse> Items);

public sealed record SaleListDetailResponse(
    Guid ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
