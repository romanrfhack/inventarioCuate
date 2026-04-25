namespace RefaccionariaCuate.Api.Contracts.InitialLoad;

public sealed record InitialLoadPreviewSummaryResponse(
    Guid LoadId,
    string Status,
    string ConfirmationToken,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int WarningRows,
    IReadOnlyCollection<InitialLoadPreviewRowResponse> Rows);
