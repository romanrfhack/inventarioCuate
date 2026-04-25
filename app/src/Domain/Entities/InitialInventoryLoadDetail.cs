using RefaccionariaCuate.Domain.Common;

namespace RefaccionariaCuate.Domain.Entities;

public sealed class InitialInventoryLoadDetail : Entity
{
    public Guid InitialInventoryLoadId { get; set; }
    public int SourceRow { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Supplier { get; set; }
    public decimal InitialStock { get; set; }
    public decimal? Cost { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Unit { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid? MatchedProductId { get; set; }
    public string RowStatus { get; set; } = "pending";
    public string? ReviewReason { get; set; }

    public InitialInventoryLoad InitialInventoryLoad { get; set; } = null!;
}
