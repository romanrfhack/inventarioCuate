using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class InitialLoadFlowValidationTests
{
    [Fact]
    public async Task Applied_Load_Should_Be_Queryable_And_Consistent_In_Persistence()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);

        var product = new Product
        {
            PrimaryCode = "SL4-CHECK-001",
            InternalKey = "SL4-CHECK-001",
            Description = "Producto validado",
            CurrentCost = 15,
            CurrentSalePrice = 20,
            Status = "activo"
        };

        var load = new InitialInventoryLoad
        {
            FileName = "slice4-check.csv",
            Status = "applied",
            LoadType = "manual_csv",
            ConfirmationToken = "TOKEN",
            SummaryJson = "{\"rows\":1,\"createdProducts\":1,\"createdBalances\":1,\"createdMovements\":1}",
            UserId = Guid.NewGuid()
        };

        load.Details.Add(new InitialInventoryLoadDetail
        {
            MatchedProductId = product.Id,
            SourceRow = 2,
            Code = "SL4-CHECK-001",
            Description = "Producto validado",
            InitialStock = 7,
            RowStatus = "valid"
        });

        db.Products.Add(product);
        db.InitialInventoryLoads.Add(load);

        db.InventoryBalances.Add(new InventoryBalance
        {
            ProductId = product.Id,
            CurrentStock = 7,
            BaseOrigin = $"initial_load:{load.Id}"
        });

        db.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = product.Id,
            MovementType = "carga_inicial",
            Quantity = 7,
            ResultingStock = 7,
            SourceType = "initial_load",
            SourceId = load.Id.ToString()
        });

        await db.SaveChangesAsync();

        var persistedLoad = await db.InitialInventoryLoads.Include(x => x.Details).SingleAsync();
        var persistedBalance = await db.InventoryBalances.SingleAsync();
        var persistedMovement = await db.InventoryMovements.SingleAsync();

        persistedLoad.Status.Should().Be("applied");
        persistedLoad.Details.Should().ContainSingle();
        persistedLoad.Details.Single().RowStatus.Should().Be("valid");
        persistedBalance.CurrentStock.Should().Be(7);
        persistedBalance.BaseOrigin.Should().Be($"initial_load:{load.Id}");
        persistedMovement.MovementType.Should().Be("carga_inicial");
        persistedMovement.SourceType.Should().Be("initial_load");
        persistedMovement.SourceId.Should().Be(load.Id.ToString());
    }
}
