namespace RefaccionariaCuate.Api.Contracts.Demo;

public sealed record DemoResetRequest(string ConfirmationText, string Reason, bool ReseedAfterReset = true);
