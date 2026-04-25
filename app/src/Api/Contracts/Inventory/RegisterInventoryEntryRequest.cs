namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed class RegisterInventoryEntryRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
