using FluentAssertions;
using RefaccionariaCuate.Infrastructure.Services;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class SupplierCatalogParserTests
{
    private readonly SupplierCatalogSpreadsheetParser _parser = new();

    [Theory]
    [InlineData("Alessia", "alessia", "alessia/alessia-fixture.xlsx")]
    [InlineData("Masuda", "masuda", "masuda/masuda-fixture.xlsx")]
    [InlineData("C-CEDIS", "c-cedis", "c-cedis/c-cedis-fixture.xlsx")]
    public void Parse_Should_Normalize_Known_Provider_File(string supplierName, string profile, string relativeFixturePath)
    {
        var filePath = GetFixturePath(relativeFixturePath);
        using var stream = File.OpenRead(filePath);
        var result = _parser.Parse(supplierName, profile, Path.GetFileName(filePath), stream);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().NotBeEmpty();
        result.Rows.Should().OnlyContain(x => x.ImportProfile == profile && x.SupplierName == supplierName);
        result.Rows.Should().Contain(x => !string.IsNullOrWhiteSpace(x.Description));
    }

    private static string GetFixturePath(string relativeFixturePath)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../data/provider-catalogs/fixtures", relativeFixturePath));
    }
}
