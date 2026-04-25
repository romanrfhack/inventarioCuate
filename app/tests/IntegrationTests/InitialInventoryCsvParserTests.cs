using FluentAssertions;
using RefaccionariaCuate.Infrastructure.Services;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class InitialInventoryCsvParserTests
{
    private readonly InitialInventoryCsvParser _parser = new();

    [Fact]
    public void Parse_Should_Return_Valid_And_Warning_Rows()
    {
        var csv = "codigo,descripcion,marca,proveedor,costo,precio_venta,existencia_inicial,unidad,ubicacion,observaciones\n" +
                  "ABC-001,Producto A,Marca A,Proveedor A,10.5,15.0,5,pieza,A1,ok\n" +
                  ",Producto B,Marca B,,12.0,,3,pieza,B1,sin precio";

        var result = _parser.Parse(csv);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Rows.Should().HaveCount(2);
        result.Rows.Should().Contain(x => x.RowStatus == "valid");
        result.Rows.Should().Contain(x => x.RowStatus == "warning");
    }

    [Fact]
    public void Parse_Should_Fail_Row_When_InitialStock_Is_Invalid()
    {
        var csv = "codigo,descripcion,existencia_inicial\n" +
                  "ABC-001,Producto A,-1\n";

        var result = _parser.Parse(csv);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().ContainSingle();
        result.Rows.Single().RowStatus.Should().Be("invalid");
        result.Rows.Single().ReviewReason.Should().Contain("existencia_inicial no puede ser negativa");
    }

    [Fact]
    public void Parse_Should_Fail_When_Required_Columns_Are_Missing()
    {
        var csv = "codigo,marca\nABC-001,Marca A\n";

        var result = _parser.Parse(csv);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Contains("Faltan columnas obligatorias"));
    }
}
