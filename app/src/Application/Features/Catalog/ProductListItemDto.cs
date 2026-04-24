namespace RefaccionariaCuate.Application.Features.Catalog;

public sealed record ProductListItemDto(Guid Id, string InternalKey, string? PrimaryCode, string Description, string? Brand, decimal Stock, decimal? SalePrice, bool RequiresReview);
