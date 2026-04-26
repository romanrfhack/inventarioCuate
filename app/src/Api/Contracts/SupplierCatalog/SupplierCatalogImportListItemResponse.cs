namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportListItemResponse(
    Guid BatchId,
    string SupplierName,
    string FileName,
    string Status,
    DateTimeOffset CreatedAt,
    int TotalRows,
    int ReadyRows,
    int WarningRows,
    int ConflictRows,
    int AppliedRows);
