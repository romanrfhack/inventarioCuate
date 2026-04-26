namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed record SupplierCatalogImportPreviewRequest(string SupplierName, string FileName, string CsvContent);
