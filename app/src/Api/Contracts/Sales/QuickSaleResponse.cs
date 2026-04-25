namespace RefaccionariaCuate.Api.Contracts.Sales;

public sealed record QuickSaleResponse(
    Guid SaleId,
    string Folio,
    decimal Total,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<QuickSaleDetailResponse> Items);

public sealed record QuickSaleDetailResponse(
    Guid ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal RemainingStock);
