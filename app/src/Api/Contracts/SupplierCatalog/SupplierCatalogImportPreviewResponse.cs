namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportPreviewResponse(
    Guid BatchId,
    string SupplierName,
    string Status,
    string ConfirmationToken,
    int TotalRows,
    int ReadyRows,
    int WarningRows,
    int ConflictRows,
    int NewProducts,
    int MatchedProducts,
    IReadOnlyCollection<SupplierCatalogImportRowResponse> Rows);
