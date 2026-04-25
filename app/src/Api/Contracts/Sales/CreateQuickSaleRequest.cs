namespace RefaccionariaCuate.Api.Contracts.Sales;

public sealed class CreateQuickSaleRequest
{
    public List<CreateQuickSaleItemRequest> Items { get; set; } = new();
}

public sealed class CreateQuickSaleItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
