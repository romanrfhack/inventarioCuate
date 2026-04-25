namespace RefaccionariaCuate.Api.Contracts.Inventory;

public sealed class RegisterInventoryAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public decimal QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
}
