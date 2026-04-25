using System.Globalization;
using System.Text;
using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Infrastructure.Services;

public sealed class InitialInventoryCsvParser
{
    private static readonly string[] RequiredColumns = ["descripcion", "existencia_inicial"];
    private static readonly string[] ExpectedColumns = ["codigo", "descripcion", "marca", "proveedor", "costo", "precio_venta", "existencia_inicial", "unidad", "ubicacion", "observaciones"];

    public ParseResult Parse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ParseResult([], ["El contenido CSV está vacío"], false);
        }

        var lines = csvContent
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

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

        var headerMap = headers
            .Select((header, index) => new { header, index })
            .ToDictionary(x => x.header, x => x.index);

        var rows = new List<InitialInventoryLoadDetail>();
        var errors = new List<string>();

        for (var i = 1; i < lines.Length; i++)
        {
            var rowNumber = i + 1;
            var values = SplitCsvLine(lines[i]);
            string Get(string column) => headerMap.TryGetValue(column, out var index) && index < values.Count ? values[index].Trim() : string.Empty;

            var description = Get("descripcion");
            var code = EmptyToNull(Get("codigo"));
            var brand = EmptyToNull(Get("marca"));
            var supplier = EmptyToNull(Get("proveedor"));
            var unit = EmptyToNull(Get("unidad"));
            var location = EmptyToNull(Get("ubicacion"));
            var notes = EmptyToNull(Get("observaciones"));
            var costText = Get("costo");
            var salePriceText = Get("precio_venta");
            var stockText = Get("existencia_inicial");

            var rowIssues = new List<string>();
            var rowStatus = "valid";

            if (string.IsNullOrWhiteSpace(description))
            {
                rowIssues.Add("descripcion obligatoria");
                rowStatus = "invalid";
            }

            if (!TryParseDecimal(stockText, out var initialStock))
            {
                rowIssues.Add("existencia_inicial inválida");
                rowStatus = "invalid";
            }
            else if (initialStock < 0)
            {
                rowIssues.Add("existencia_inicial no puede ser negativa");
                rowStatus = "invalid";
            }

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(costText))
            {
                if (!TryParseDecimal(costText, out var parsedCost))
                {
                    rowIssues.Add("costo inválido");
                    rowStatus = "invalid";
                }
                else if (parsedCost < 0)
                {
                    rowIssues.Add("costo no puede ser negativo");
                    rowStatus = "invalid";
                }
                else
                {
                    cost = parsedCost;
                }
            }

            decimal? salePrice = null;
            if (!string.IsNullOrWhiteSpace(salePriceText))
            {
                if (!TryParseDecimal(salePriceText, out var parsedSalePrice))
                {
                    rowIssues.Add("precio_venta inválido");
                    rowStatus = "invalid";
                }
                else if (parsedSalePrice < 0)
                {
                    rowIssues.Add("precio_venta no puede ser negativo");
                    rowStatus = "invalid";
                }
                else
                {
                    salePrice = parsedSalePrice;
                }
            }

            if (rowStatus != "invalid")
            {
                var warnings = new List<string>();
                if (string.IsNullOrWhiteSpace(code)) warnings.Add("sin_codigo");
                if (!salePrice.HasValue) warnings.Add("sin_precio_venta");
                if (!cost.HasValue) warnings.Add("sin_costo");
                if (string.IsNullOrWhiteSpace(supplier)) warnings.Add("sin_proveedor");
                if (warnings.Count != 0)
                {
                    rowStatus = "warning";
                    rowIssues.AddRange(warnings);
                }
            }

            rows.Add(new InitialInventoryLoadDetail
            {
                SourceRow = rowNumber,
                Code = code,
                Description = description,
                Brand = brand,
                Supplier = supplier,
                Cost = cost,
                SalePrice = salePrice,
                InitialStock = initialStock,
                Unit = unit,
                Location = location,
                Notes = notes,
                RowStatus = rowStatus,
                ReviewReason = rowIssues.Count == 0 ? null : string.Join(";", rowIssues)
            });
        }

        return new ParseResult(rows, errors, true);
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

    public sealed record ParseResult(IReadOnlyCollection<InitialInventoryLoadDetail> Rows, IReadOnlyCollection<string> Errors, bool IsSuccess);
}
