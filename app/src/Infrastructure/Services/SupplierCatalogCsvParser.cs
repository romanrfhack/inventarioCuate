using System.Globalization;
using System.Text;
using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Infrastructure.Services;

public sealed class SupplierCatalogCsvParser
{
    private static readonly string[] RequiredColumns = ["descripcion"];

    public ParseResult Parse(string supplierName, string csvContent)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            return new ParseResult([], ["El proveedor es obligatorio"], false);
        }

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ParseResult([], ["El contenido CSV está vacío"], false);
        }

        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return new ParseResult([], ["El archivo CSV debe incluir encabezado y al menos una fila de datos"], false);
        }

        var headers = SplitCsvLine(lines[0]).Select(Normalize).ToArray();
        var missingRequired = RequiredColumns.Where(required => !headers.Contains(required)).ToList();
        if (missingRequired.Count != 0)
        {
            return new ParseResult([], [$"Faltan columnas obligatorias: {string.Join(", ", missingRequired)}"], false);
        }

        var headerMap = headers.Select((header, index) => new { header, index }).ToDictionary(x => x.header, x => x.index);
        var rows = new List<SupplierCatalogImportDetail>();

        for (var i = 1; i < lines.Length; i++)
        {
            var rowNumber = i + 1;
            var values = SplitCsvLine(lines[i]);
            string Get(string column) => headerMap.TryGetValue(column, out var index) && index < values.Count ? values[index].Trim() : string.Empty;

            var description = Get("descripcion");
            var supplierProductCode = EmptyToNull(Get("codigo")) ?? EmptyToNull(Get("codigo_proveedor"));
            var brand = EmptyToNull(Get("marca"));
            var unit = EmptyToNull(Get("unidad"));
            var costText = Get("costo");
            var priceText = Get("precio_venta");
            if (string.IsNullOrWhiteSpace(priceText))
            {
                priceText = Get("precio_sugerido");
            }

            var rowIssues = new List<string>();
            var rowStatus = "ready";

            if (string.IsNullOrWhiteSpace(description))
            {
                rowIssues.Add("descripcion obligatoria");
                rowStatus = "invalid";
            }

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(costText))
            {
                if (!TryParseDecimal(costText, out var parsedCost) || parsedCost < 0)
                {
                    rowIssues.Add("costo inválido");
                    rowStatus = "invalid";
                }
                else
                {
                    cost = parsedCost;
                }
            }

            decimal? suggestedSalePrice = null;
            if (!string.IsNullOrWhiteSpace(priceText))
            {
                if (!TryParseDecimal(priceText, out var parsedPrice) || parsedPrice < 0)
                {
                    rowIssues.Add("precio sugerido inválido");
                    rowStatus = "invalid";
                }
                else
                {
                    suggestedSalePrice = parsedPrice;
                }
            }

            if (rowStatus != "invalid" && string.IsNullOrWhiteSpace(supplierProductCode))
            {
                rowStatus = "warning";
                rowIssues.Add("sin_codigo_proveedor");
            }

            rows.Add(new SupplierCatalogImportDetail
            {
                SourceRow = rowNumber,
                SupplierProductCode = supplierProductCode,
                Description = description,
                Brand = brand,
                Cost = cost,
                SuggestedSalePrice = suggestedSalePrice,
                Unit = unit,
                RowStatus = rowStatus,
                ReviewReason = rowIssues.Count == 0 ? null : string.Join(";", rowIssues)
            });
        }

        return new ParseResult(rows, [], true);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(value, NumberStyles.Number, new CultureInfo("es-MX"), out result);
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Normalize(string header) => header.Trim().ToLowerInvariant();

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

    public sealed record ParseResult(IReadOnlyCollection<SupplierCatalogImportDetail> Rows, IReadOnlyCollection<string> Errors, bool IsSuccess);
}
