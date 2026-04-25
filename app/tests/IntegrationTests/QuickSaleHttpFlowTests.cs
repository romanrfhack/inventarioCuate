using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefaccionariaCuate.Api.Controllers;
using RefaccionariaCuate.Api.Contracts.Sales;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class QuickSaleHttpFlowTests
{
    [Fact]
    public async Task QuickSale_Should_Create_Sale_Decrease_Stock_And_Register_Movement()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var stockBefore = product.InventoryBalance!.CurrentStock;

        var controller = CreateController(db);
        var result = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = product.Id, Quantity = 2m }]
        }, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<QuickSaleResponse>().Subject;

        response.Folio.Should().StartWith("VTA-");
        response.Total.Should().BeGreaterThan(0);
        response.Items.Should().ContainSingle();
        response.Items.First().RemainingStock.Should().Be(stockBefore - 2m);

        var sale = await db.Sales.Include(x => x.Details).SingleAsync(x => x.Id == response.SaleId);
        sale.Status.Should().Be("confirmed");
        sale.Details.Should().ContainSingle();
        sale.Details[0].Quantity.Should().Be(2m);

        var balance = await db.InventoryBalances.SingleAsync(x => x.ProductId == product.Id);
        balance.CurrentStock.Should().Be(stockBefore - 2m);

        var movement = await db.InventoryMovements.SingleAsync(x => x.SourceId == sale.Id.ToString());
        movement.MovementType.Should().Be("venta");
        movement.Quantity.Should().Be(2m);
        movement.ResultingStock.Should().Be(stockBefore - 2m);
    }

    [Fact]
    public async Task QuickSale_Should_Return_Conflict_When_Stock_Is_Insufficient()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var controller = CreateController(db);

        var result = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = product.Id, Quantity = product.InventoryBalance!.CurrentStock + 1m }]
        }, CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().NotBeNull();
        conflict.Value!.GetType().GetProperty("code")!.GetValue(conflict.Value)!.Should().Be("409_INSUFFICIENT_STOCK");
        (await db.Sales.CountAsync()).Should().Be(0);
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

    private static SalesController CreateController(ApplicationDbContext db)
    {
        var controller = new SalesController(db)
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

        return controller;
    }
}
