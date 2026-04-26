using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Services;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class SupplierCatalogMatcherTests
{
    [Fact]
    public async Task MatchAsync_Should_Detect_Code_Conflict_When_Description_Points_To_Different_Product()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source=/tmp/refaccionaria-cuate-matcher-{Guid.NewGuid():N}.db")
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var productByCode = new Product
        {
            InternalKey = "P-001",
            PrimaryCode = "CODE-001",
            Description = "Balata delantera",
            Brand = "Marca A"
        };

        var productByDescription = new Product
        {
            InternalKey = "P-002",
            PrimaryCode = "CODE-002",
            Description = "Filtro de aceite",
            Brand = "Marca B"
        };

        await db.Products.AddRangeAsync(productByCode, productByDescription);
        await db.SaveChangesAsync();

        var matcher = new SupplierCatalogMatcher(db);
        var detail = new SupplierCatalogImportDetail
        {
            SourceRow = 2,
            SupplierProductCode = "CODE-001",
            Description = "Filtro de aceite",
            Brand = "Marca B",
            Cost = 100,
            SuggestedSalePrice = 150,
            RowStatus = "ready"
        };

        await matcher.MatchAsync([detail], CancellationToken.None);

        detail.RowStatus.Should().Be("conflict");
        detail.MatchType.Should().Be("conflict");
        detail.ActionType.Should().Be("review");
        detail.ReviewReason.Should().Contain("codigo_y_descripcion_apuntan_a_productos_distintos");
    }
}
