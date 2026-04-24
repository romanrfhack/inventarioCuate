namespace RefaccionariaCuate.Api.Contracts.Catalog;

public sealed record ProductResponse(Guid Id, string InternalKey, string? PrimaryCode, string Description, string? Brand, decimal Stock, decimal? SalePrice, bool RequiresReview);
