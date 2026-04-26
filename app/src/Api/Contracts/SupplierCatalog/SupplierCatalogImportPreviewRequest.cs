using Microsoft.AspNetCore.Http;

namespace RefaccionariaCuate.Api.Contracts.SupplierCatalog;

public sealed class SupplierCatalogImportPreviewRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string ImportProfile { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
