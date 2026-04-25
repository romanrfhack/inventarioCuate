using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefaccionariaCuate.Api.Contracts.Inventory;
using RefaccionariaCuate.Api.Controllers;
using RefaccionariaCuate.Domain.Enums;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class InventoryMovementsControllerTests
{
    [Fact]
    public async Task RegisterEntry_Should_Increase_Stock_And_Create_Movement()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var stockBefore = product.InventoryBalance!.CurrentStock;
        var controller = CreateController(db);

        var result = await controller.RegisterEntry(new RegisterInventoryEntryRequest
        {
            ProductId = product.Id,
            Quantity = 5m,
            Reason = "Entrada por compra mostrador"
        }, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<InventoryMovementResponse>().Subject;
        response.MovementType.Should().Be(MovementType.EntradaManual);
        response.ResultingStock.Should().Be(stockBefore + 5m);

        var balance = await db.InventoryBalances.SingleAsync(x => x.ProductId == product.Id);
        balance.CurrentStock.Should().Be(stockBefore + 5m);

        var movement = (await db.InventoryMovements.Where(x => x.ProductId == product.Id).ToListAsync())
            .OrderByDescending(x => x.CreatedAt)
            .First();
        movement.MovementType.Should().Be(MovementType.EntradaManual);
        movement.Reason.Should().Be("Entrada por compra mostrador");
        movement.ResultingStock.Should().Be(stockBefore + 5m);
    }

    [Fact]
    public async Task RegisterAdjustment_Should_Reject_When_Resulting_Stock_Is_Negative()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var stockBefore = product.InventoryBalance!.CurrentStock;
        var controller = CreateController(db);

        var result = await controller.RegisterAdjustment(new RegisterInventoryAdjustmentRequest
        {
            ProductId = product.Id,
            QuantityDelta = -(stockBefore + 1m),
            Reason = "Conteo físico con faltante"
        }, CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().NotBeNull();
        conflict.Value!.GetType().GetProperty("code")!.GetValue(conflict.Value)!.Should().Be("409_NEGATIVE_STOCK_NOT_ALLOWED");

        var balance = await db.InventoryBalances.SingleAsync(x => x.ProductId == product.Id);
        balance.CurrentStock.Should().Be(stockBefore);
        (await db.InventoryMovements.CountAsync(x => x.ProductId == product.Id && x.MovementType == MovementType.AjusteManual)).Should().Be(0);
    }

    [Fact]
    public async Task RegisterAdjustment_Should_Require_Reason_And_Allow_Negative_Delta_When_Stock_Remains_NonNegative()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var stockBefore = product.InventoryBalance!.CurrentStock;
        var controller = CreateController(db);

        var invalidResult = await controller.RegisterAdjustment(new RegisterInventoryAdjustmentRequest
        {
            ProductId = product.Id,
            QuantityDelta = -1m,
            Reason = "   "
        }, CancellationToken.None);

        invalidResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var validResult = await controller.RegisterAdjustment(new RegisterInventoryAdjustmentRequest
        {
            ProductId = product.Id,
            QuantityDelta = -1m,
            Reason = "Ajuste por conteo físico"
        }, CancellationToken.None);

        var ok = validResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<InventoryMovementResponse>().Subject;
        response.MovementType.Should().Be(MovementType.AjusteManual);
        response.Quantity.Should().Be(-1m);
        response.ResultingStock.Should().Be(stockBefore - 1m);
    }

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Demo:DefaultAdminPassword"] = "Demo123!"
            })
            .Build();

        var seed = new DemoSeedService(db, configuration);
        await seed.EnsureSeededAsync();
    }

    private static InventoryController CreateController(ApplicationDbContext db)
    {
        return new InventoryController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    ], "TestAuth"))
                }
            }
        };
    }
}
