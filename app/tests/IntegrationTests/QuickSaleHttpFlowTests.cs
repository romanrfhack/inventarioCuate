using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefaccionariaCuate.Api.Contracts.Sales;
using RefaccionariaCuate.Api.Controllers;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class QuickSaleHttpFlowTests
{
    [Fact]
    public async Task QuickSale_Should_Create_Sale_With_Multiple_Items_Decrease_Stock_And_Register_Movements()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var products = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).Take(2).ToListAsync();
        var firstProduct = products[0];
        var secondProduct = products[1];
        var stockBeforeFirst = firstProduct.InventoryBalance!.CurrentStock;
        var stockBeforeSecond = secondProduct.InventoryBalance!.CurrentStock;

        var controller = CreateController(db);
        var result = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items =
            [
                new CreateQuickSaleItemRequest { ProductId = firstProduct.Id, Quantity = 2m },
                new CreateQuickSaleItemRequest { ProductId = firstProduct.Id, Quantity = 1m },
                new CreateQuickSaleItemRequest { ProductId = secondProduct.Id, Quantity = 1m }
            ]
        }, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<QuickSaleResponse>().Subject;

        response.Folio.Should().StartWith("VTA-");
        response.Items.Should().HaveCount(3);
        response.Items.Count(x => x.ProductId == firstProduct.Id).Should().Be(2);
        response.Items.Last(x => x.ProductId == firstProduct.Id).RemainingStock.Should().Be(stockBeforeFirst - 3m);
        response.Items.Single(x => x.ProductId == secondProduct.Id).RemainingStock.Should().Be(stockBeforeSecond - 1m);

        var sale = await db.Sales.Include(x => x.Details).SingleAsync(x => x.Id == response.SaleId);
        sale.Status.Should().Be("confirmed");
        sale.Details.Should().HaveCount(3);

        var balanceFirst = await db.InventoryBalances.SingleAsync(x => x.ProductId == firstProduct.Id);
        var balanceSecond = await db.InventoryBalances.SingleAsync(x => x.ProductId == secondProduct.Id);
        balanceFirst.CurrentStock.Should().Be(stockBeforeFirst - 3m);
        balanceSecond.CurrentStock.Should().Be(stockBeforeSecond - 1m);

        var movements = await db.InventoryMovements.Where(x => x.SourceId == sale.Id.ToString()).ToListAsync();
        movements.Should().HaveCount(3);
        movements.Should().OnlyContain(x => x.MovementType == "venta");
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

    [Fact]
    public async Task CancelSale_Should_Restore_Stock_Register_Reversal_And_Appear_In_List()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var stockBefore = product.InventoryBalance!.CurrentStock;
        var controller = CreateController(db);

        var quickSale = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = product.Id, Quantity = 2m }]
        }, CancellationToken.None);
        var saleResponse = ((OkObjectResult)quickSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;

        var cancelResult = await controller.CancelSale(saleResponse.SaleId, CancellationToken.None);
        var cancelOk = cancelResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var cancelResponse = cancelOk.Value.Should().BeOfType<CancelSaleResponse>().Subject;

        cancelResponse.Status.Should().Be("cancelled");
        cancelResponse.Items.Should().ContainSingle();
        cancelResponse.Items.First().ResultingStock.Should().Be(stockBefore);

        var sale = await db.Sales.SingleAsync(x => x.Id == saleResponse.SaleId);
        sale.Status.Should().Be("cancelled");

        var balance = await db.InventoryBalances.SingleAsync(x => x.ProductId == product.Id);
        balance.CurrentStock.Should().Be(stockBefore);

        var movements = await db.InventoryMovements.Where(x => x.SourceId == sale.Id.ToString()).ToListAsync();
        movements.Should().HaveCount(2);
        movements.Should().Contain(x => x.MovementType == "venta_cancelacion" && x.ResultingStock == stockBefore);

        var listResult = await controller.GetSales(null, null, null, null, CancellationToken.None);
        var listOk = listResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = listOk.Value.Should().BeAssignableTo<IReadOnlyCollection<SaleListItemResponse>>().Subject;
        list.Should().ContainSingle(x => x.SaleId == sale.Id && x.Status == "cancelled");
    }

    [Fact]
    public async Task GetSaleDetail_Should_Return_Items_And_Summary_For_Existing_Sale()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var products = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).Take(2).ToListAsync();
        var controller = CreateController(db);

        var quickSale = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items =
            [
                new CreateQuickSaleItemRequest { ProductId = products[0].Id, Quantity = 1m },
                new CreateQuickSaleItemRequest { ProductId = products[1].Id, Quantity = 2m }
            ]
        }, CancellationToken.None);
        var saleResponse = ((OkObjectResult)quickSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;

        var detailResult = await controller.GetSaleDetail(saleResponse.SaleId, CancellationToken.None);
        var detailOk = detailResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var detail = detailOk.Value.Should().BeOfType<SaleDetailResponse>().Subject;

        detail.SaleId.Should().Be(saleResponse.SaleId);
        detail.ItemCount.Should().Be(2);
        detail.TotalQuantity.Should().Be(3m);
        detail.Items.Should().HaveCount(2);
        detail.Items.Should().Contain(x => x.ProductId == products[0].Id && x.Quantity == 1m);
        detail.Items.Should().Contain(x => x.ProductId == products[1].Id && x.Quantity == 2m);
    }

    [Fact]
    public async Task GetSales_Should_Filter_By_Status_Folio_And_Date_Range()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var product = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).FirstAsync();
        var controller = CreateController(db);

        var firstSale = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = product.Id, Quantity = 1m }]
        }, CancellationToken.None);
        var firstResponse = ((OkObjectResult)firstSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;

        await Task.Delay(20);

        var secondSale = await controller.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = product.Id, Quantity = 1m }]
        }, CancellationToken.None);
        var secondResponse = ((OkObjectResult)secondSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;

        await controller.CancelSale(secondResponse.SaleId, CancellationToken.None);

        var allForToday = await controller.GetSales(null, null, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);
        var allTodayOk = allForToday.Result.Should().BeOfType<OkObjectResult>().Subject;
        var allToday = allTodayOk.Value.Should().BeAssignableTo<IReadOnlyCollection<SaleListItemResponse>>().Subject;
        allToday.Should().HaveCount(2);

        var cancelledOnly = await controller.GetSales(null, "cancelled", null, null, CancellationToken.None);
        var cancelledOk = cancelledOnly.Result.Should().BeOfType<OkObjectResult>().Subject;
        var cancelledList = cancelledOk.Value.Should().BeAssignableTo<IReadOnlyCollection<SaleListItemResponse>>().Subject;
        cancelledList.Should().ContainSingle(x => x.SaleId == secondResponse.SaleId && x.Status == "cancelled");

        var folioFiltered = await controller.GetSales(secondResponse.Folio[^4..], null, null, null, CancellationToken.None);
        var folioOk = folioFiltered.Result.Should().BeOfType<OkObjectResult>().Subject;
        var folioList = folioOk.Value.Should().BeAssignableTo<IReadOnlyCollection<SaleListItemResponse>>().Subject;
        folioList.Should().ContainSingle(x => x.SaleId == secondResponse.SaleId);
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
