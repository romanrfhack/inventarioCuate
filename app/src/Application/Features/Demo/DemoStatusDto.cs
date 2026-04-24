namespace RefaccionariaCuate.Application.Features.Demo;

public sealed record DemoStatusDto(bool AllowReset, string Environment, int ProductCount, int UserCount, int PendingInitialLoads);
