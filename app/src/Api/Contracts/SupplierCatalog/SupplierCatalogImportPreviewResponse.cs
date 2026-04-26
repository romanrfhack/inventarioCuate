namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportPreviewResponse(
    Guid BatchId,
    string SupplierName,
    string ImportProfile,
    string FileName,
    string Status,
    string ConfirmationToken,
    int TotalRows,
    int MatchCodigoRows,
    int ProductoNuevoRows,
    int DatoIncompletoRows,
    int RequiereRevisionRows,
    int AppliedRows,
    IReadOnlyCollection<SupplierCatalogImportRowResponse> Rows);
