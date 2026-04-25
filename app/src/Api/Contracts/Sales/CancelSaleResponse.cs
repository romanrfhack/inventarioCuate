namespace RefaccionariaCuate.Api.Contracts.Sales;

public sealed record CancelSaleResponse(
    Guid SaleId,
    string Folio,
    string Status,
    DateTimeOffset CancelledAt,
    IReadOnlyCollection<CancelledSaleItemResponse> Items);

public sealed record CancelledSaleItemResponse(
    Guid ProductId,
    string Description,
    decimal RestoredQuantity,
    decimal ResultingStock);
