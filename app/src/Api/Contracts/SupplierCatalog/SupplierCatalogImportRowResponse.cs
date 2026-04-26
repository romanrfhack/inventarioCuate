namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportRowResponse(
    int SourceRow,
    string? SupplierProductCode,
    string Description,
    string? Brand,
    decimal? Cost,
    decimal? SuggestedSalePrice,
    string MatchType,
    string ActionType,
    string RowStatus,
    Guid? MatchedProductId,
    decimal? ProposedCost,
    decimal? ProposedSalePrice,
    bool ApplySelected,
    string? ReviewReason);
