namespace RefaccionariaCuate.Api.Contracts.InitialLoad;

public sealed record InitialLoadListItemResponse(
    Guid LoadId,
    string FileName,
    string Status,
    string LoadType,
    DateTimeOffset CreatedAt,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int WarningRows);
