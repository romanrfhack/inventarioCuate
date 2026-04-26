using FluentAssertions;
using RefaccionariaCuate.Infrastructure.Services;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class SupplierCatalogParserTests
{
    private readonly SupplierCatalogCsvParser _parser = new();

    [Theory]
    [InlineData("Alessia", "alessia", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/alessia/07 Abril Lista Alessia 26.xlsm")]
    [InlineData("Masuda", "masuda", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/masuda/LISTA DE PRECIO - MASUDA IMPORTADOR REGIONAL 09-ABRIL.xlsx")]
    [InlineData("C-CEDIS", "c-cedis", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/c-cedis/ListaPreciosC-CEDIS-05042026.xlsx.xls")]
    public void Parse_Should_Normalize_Known_Provider_File(string supplierName, string profile, string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var result = _parser.Parse(supplierName, profile, Path.GetFileName(filePath), stream);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().NotBeEmpty();
        result.Rows.Should().OnlyContain(x => x.ImportProfile == profile && x.SupplierName == supplierName);
        result.Rows.Should().Contain(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
