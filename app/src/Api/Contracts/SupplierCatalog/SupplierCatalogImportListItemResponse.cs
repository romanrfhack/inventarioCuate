namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportListItemResponse(
    Guid BatchId,
    string SupplierName,
    string ImportProfile,
    string FileName,
    string Status,
    DateTimeOffset CreatedAt,
    int TotalRows,
    int MatchCodigoRows,
    int ProductoNuevoRows,
    int DatoIncompletoRows,
    int RequiereRevisionRows,
    int AppliedRows);
