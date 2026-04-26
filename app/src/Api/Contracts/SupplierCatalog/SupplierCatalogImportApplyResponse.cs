namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportApplyResponse(
    Guid BatchId,
    string Status,
    int UpdatedProducts,
    int CreatedProducts,
    int SkippedRows,
    int RequiereRevisionRows);
