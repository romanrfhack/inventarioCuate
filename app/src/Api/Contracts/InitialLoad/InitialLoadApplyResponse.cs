namespace RefaccionariaCuate.Api.Contracts.InitialLoad;

public sealed record InitialLoadApplyResponse(
    Guid LoadId,
    string Status,
    int CreatedProducts,
    int MatchedProducts,
    int CreatedInventoryBalances,
    int CreatedMovements,
    int WarningRows);
