namespace RefaccionariaCuate.Api.Contracts.InitialLoad;

public sealed class InitialLoadPreviewRequest
{
    public string FileName { get; set; } = "inventario_inicial.csv";
    public string CsvContent { get; set; } = string.Empty;
}
