namespace RefaccionariaCuate.Api.Contracts.InitialLoad;

public sealed record InitialLoadPreviewRowResponse(
    int SourceRow,
    string? Code,
    string Description,
    decimal InitialStock,
    string RowStatus,
    string? ReviewReason);
