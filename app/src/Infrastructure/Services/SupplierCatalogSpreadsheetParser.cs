using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Infrastructure.Services;

public sealed class SupplierCatalogSpreadsheetParser
{
    private static readonly IReadOnlyDictionary<string, ProfileDefinition> Profiles = new Dictionary<string, ProfileDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["alessia"] = new(
            "alessia",
            "Alessia",
            ["Ale 25", "Pr", "st"],
            "Ale 25",
            5,
            static (context, row) => ParseAlessia(context, row)),
        ["masuda"] = new(
            "masuda",
            "Masuda",
            ["COMPRA", "ZONA 17"],
            "COMPRA",
            5,
            static (context, row) => ParseMasuda(context, row)),
        ["c-cedis"] = new(
            "c-cedis",
            "C-CEDIS",
            ["Hoja1", "Hoja3"],
            "Hoja1",
            9,
            static (context, row) => ParseCCedis(context, row))
    };

    static SupplierCatalogSpreadsheetParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public IReadOnlyCollection<ImportProfileDescriptor> GetProfiles()
    {
        return Profiles.Values
            .Select(x => new ImportProfileDescriptor(x.Key, x.SupplierName, x.PreferredSheet, x.CandidateSheets))
            .OrderBy(x => x.SupplierName)
            .ToList();
    }

    public ParseResult Parse(string supplierName, string importProfile, string fileName, Stream fileStream)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            return new ParseResult([], ["El proveedor es obligatorio"], false);
        }

        if (string.IsNullOrWhiteSpace(importProfile) || !Profiles.TryGetValue(importProfile.Trim(), out var profile))
        {
            return new ParseResult([], ["El perfil de importación no está soportado"], false);
        }

        if (fileStream.Length == 0)
        {
            return new ParseResult([], ["El archivo está vacío"], false);
        }

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        if (dataSet.Tables.Count == 0)
        {
            return new ParseResult([], ["No se detectaron hojas en el archivo"], false);
        }

        var tableMap = dataSet.Tables.Cast<DataTable>().ToDictionary(x => x.TableName, StringComparer.OrdinalIgnoreCase);
        var primarySheet = profile.CandidateSheets.FirstOrDefault(tableMap.ContainsKey);
        if (primarySheet is null)
        {
            return new ParseResult([], [$"No se encontró una hoja compatible para el perfil {profile.Key}"], false);
        }

        var primaryTable = tableMap[primarySheet];
        var context = new ParseContext(supplierName.Trim(), profile, primarySheet, primaryTable, tableMap);
        var rows = new List<SupplierCatalogImportDetail>();

        for (var index = profile.HeaderRowNumber; index < primaryTable.Rows.Count; index++)
        {
            var excelRowNumber = index + 1;
            var row = primaryTable.Rows[index];
            var detail = profile.RowParser(context, new ExcelRow(excelRowNumber, row));
            if (detail is null)
            {
                continue;
            }

            detail.SupplierName = context.SupplierName;
            detail.ImportProfile = profile.Key;
            detail.SourceSheet = primarySheet;
            rows.Add(detail);
        }

        if (rows.Count == 0)
        {
            return new ParseResult([], ["No se detectaron filas de datos útiles para el perfil seleccionado"], false);
        }

        return new ParseResult(rows, [], true);
    }

    private static SupplierCatalogImportDetail? ParseAlessia(ParseContext context, ExcelRow row)
    {
        var code = row.GetString(3);
        var description = row.GetString(7);
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        if (LooksLikeSection(description, row))
        {
            return null;
        }

        var stockByCode = LoadAlessiaStock(context);
        stockByCode.TryGetValue(NormalizeCode(code), out var stockText);

        return new SupplierCatalogImportDetail
        {
            SourceRow = row.RowNumber,
            SupplierProductCode = EmptyToNull(code),
            Description = description,
            Brand = EmptyToNull(row.GetString(4)),
            Unit = EmptyToNull(row.GetString(5)),
            PiecesPerBox = row.GetDecimal(6),
            Compatibility = EmptyToNull(row.GetString(8)),
            SuggestedSalePrice = row.GetDecimal(9),
            SupplierAvailability = row.GetDecimal(1),
            SupplierStockText = EmptyToNull(stockText) ?? EmptyToNull(row.GetString(1)),
            PriceLevelsJson = BuildPriceLevelsJson(new Dictionary<string, decimal?>
            {
                ["precio_lista"] = row.GetDecimal(9)
            }),
            RequiresRevision = string.Equals(row.GetString(11), "NUEVO", StringComparison.OrdinalIgnoreCase),
            RevisionReason = string.Equals(row.GetString(11), "NUEVO", StringComparison.OrdinalIgnoreCase) ? "marcado_como_nuevo_en_fuente" : null
        };
    }

    private static SupplierCatalogImportDetail? ParseMasuda(ParseContext context, ExcelRow row)
    {
        var code = row.GetString(1);
        var description = row.GetString(2);
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return new SupplierCatalogImportDetail
        {
            SourceRow = row.RowNumber,
            SupplierProductCode = EmptyToNull(code),
            Description = description,
            Unit = EmptyToNull(row.GetString(3)),
            Cost = row.GetDecimal(4),
            Line = EmptyToNull(row.GetString(7)),
            Family = EmptyToNull(row.GetString(8)),
            SubFamily = EmptyToNull(row.GetString(9)),
            PiecesPerBox = row.GetDecimal(10),
            SupplierStockText = EmptyToNull(row.GetString(11)),
            SupplierAvailability = ParseAvailability(row.GetString(11)),
            PriceLevelsJson = BuildPriceLevelsJson(new Dictionary<string, decimal?>
            {
                ["importador_regional"] = row.GetDecimal(4)
            })
        };
    }

    private static SupplierCatalogImportDetail? ParseCCedis(ParseContext context, ExcelRow row)
    {
        var code = row.GetString(0);
        var description = row.GetString(3);
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(description) && description.Trim().StartsWith("ACEITES", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SupplierCatalogImportDetail
        {
            SourceRow = row.RowNumber,
            SupplierProductCode = EmptyToNull(code),
            Description = description,
            Compatibility = EmptyToNull(row.GetString(5)),
            Family = EmptyToNull(row.GetString(6)),
            Category = EmptyToNull(row.GetString(7)),
            SuggestedSalePrice = row.GetDecimal(8),
            SupplierAvailability = row.GetDecimal(4),
            SupplierStockText = EmptyToNull(row.GetString(4)),
            PriceLevelsJson = BuildPriceLevelsJson(new Dictionary<string, decimal?>
            {
                ["mayoreo"] = row.GetDecimal(8),
                ["mas_de_25_mil"] = row.GetDecimal(10),
                ["mas_de_50_mil"] = row.GetDecimal(11),
                ["mas_de_100_mil"] = row.GetDecimal(12),
                ["mas_de_200_mil"] = row.GetDecimal(13),
                ["mas_de_300_mil"] = row.GetDecimal(14)
            })
        };
    }

    private static Dictionary<string, string> LoadAlessiaStock(ParseContext context)
    {
        if (context.Cache.TryGetValue("alessia-stock", out var cacheEntry) && cacheEntry is Dictionary<string, string> cached)
        {
            return cached;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!context.Tables.TryGetValue("st", out var stockTable))
        {
            context.Cache["alessia-stock"] = result;
            return result;
        }

        for (var index = 1; index < stockTable.Rows.Count; index++)
        {
            var row = new ExcelRow(index + 1, stockTable.Rows[index]);
            var code = NormalizeCode(row.GetString(0));
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            result[code] = row.GetString(1);
        }

        context.Cache["alessia-stock"] = result;
        return result;
    }

    private static bool LooksLikeSection(string description, ExcelRow row)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(row.GetString(3)))
        {
            return false;
        }

        var normalized = description.Replace(" ", string.Empty);
        return normalized.All(char.IsLetter);
    }

    private static decimal? ParseAvailability(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim().ToUpperInvariant();
        return text switch
        {
            "AGOTADO" => 0,
            "DISPONIBLE" => 1,
            _ => TryParseDecimal(text, out var parsed) ? parsed : null
        };
    }

    private static string? BuildPriceLevelsJson(Dictionary<string, decimal?> values)
    {
        var clean = values
            .Where(x => x.Value.HasValue)
            .ToDictionary(x => x.Key, x => x.Value!.Value, StringComparer.OrdinalIgnoreCase);

        return clean.Count == 0 ? null : JsonSerializer.Serialize(clean);
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        var text = value?.Trim().Replace("$", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(text, NumberStyles.Any, new CultureInfo("es-MX"), out result);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    public sealed record ParseResult(IReadOnlyCollection<SupplierCatalogImportDetail> Rows, IReadOnlyCollection<string> Errors, bool IsSuccess);
    public sealed record ImportProfileDescriptor(string Key, string SupplierName, string PreferredSheet, IReadOnlyCollection<string> CandidateSheets);

    private sealed record ProfileDefinition(
        string Key,
        string SupplierName,
        IReadOnlyCollection<string> CandidateSheets,
        string PreferredSheet,
        int HeaderRowNumber,
        Func<ParseContext, ExcelRow, SupplierCatalogImportDetail?> RowParser);

    private sealed class ParseContext(string supplierName, ProfileDefinition profile, string primarySheet, DataTable primaryTable, IReadOnlyDictionary<string, DataTable> tables)
    {
        public string SupplierName { get; } = supplierName;
        public ProfileDefinition Profile { get; } = profile;
        public string PrimarySheet { get; } = primarySheet;
        public DataTable PrimaryTable { get; } = primaryTable;
        public IReadOnlyDictionary<string, DataTable> Tables { get; } = tables;
        public Dictionary<string, object> Cache { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct ExcelRow(int RowNumber, DataRow DataRow)
    {
        public string GetString(int index)
        {
            if (index < 0 || index >= DataRow.ItemArray.Length)
            {
                return string.Empty;
            }

            return DataRow[index]?.ToString()?.Trim() ?? string.Empty;
        }

        public decimal? GetDecimal(int index)
        {
            var raw = GetString(index);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return TryParseDecimal(raw, out var value) ? value : null;
        }
    }
}
