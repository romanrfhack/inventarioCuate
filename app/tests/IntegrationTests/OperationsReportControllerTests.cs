using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefaccionariaCuate.Api.Contracts.Reports;
using RefaccionariaCuate.Api.Contracts.Sales;
using RefaccionariaCuate.Api.Controllers;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class OperationsReportControllerTests
{
    [Fact]
    public async Task GetOperationsReport_Should_Return_Inventory_RecentSales_Anomalies_And_GrossProfit_Base()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);

        var products = await db.Products.Include(x => x.InventoryBalance).OrderBy(x => x.Description).ToListAsync();
        var anomalous = products[0];
        anomalous.PrimaryCode = null;
        anomalous.CurrentCost = null;
        anomalous.RequiresReview = true;
        anomalous.ReviewReason = "costo pendiente";
        anomalous.InventoryBalance!.CurrentStock = 0m;

        var negativeStock = products[1];
        negativeStock.InventoryBalance!.CurrentStock = -2m;

        await db.SaveChangesAsync();

        var salesController = CreateSalesController(db);
        var quickSale = await salesController.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = products[2].Id, Quantity = 2m, UnitPrice = 110m }]
        }, CancellationToken.None);
        var saleResponse = ((OkObjectResult)quickSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;

        var secondSale = await salesController.CreateQuickSale(new CreateQuickSaleRequest
        {
            Items = [new CreateQuickSaleItemRequest { ProductId = products[2].Id, Quantity = 1m, UnitPrice = 110m }]
        }, CancellationToken.None);
        var secondSaleResponse = ((OkObjectResult)secondSale.Result!).Value.Should().BeOfType<QuickSaleResponse>().Subject;
        await salesController.CancelSale(secondSaleResponse.SaleId, CancellationToken.None);

        var controller = CreateReportsController(db);
        var result = await controller.GetOperationsReport(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var report = ok.Value.Should().BeOfType<OperationsReportResponse>().Subject;

        report.Summary.TotalProducts.Should().Be(3);
        report.Summary.ProductsWithStock.Should().Be(1);
        report.Summary.ProductsWithoutStock.Should().Be(1);
        report.Summary.ProductsWithNegativeStock.Should().Be(1);
        report.Summary.ConfirmedSalesCount.Should().Be(1);
        report.Summary.ConfirmedSalesTotal.Should().Be(saleResponse.Total);
        report.Summary.ConfirmedSalesGrossProfit.Should().Be(90m);

        report.RecentSales.Should().HaveCount(2);
        report.RecentSales.Should().Contain(x => x.SaleId == saleResponse.SaleId && x.GrossProfit == 90m);
        report.RecentSales.Should().Contain(x => x.SaleId == secondSaleResponse.SaleId && x.Status == "cancelled");

        report.ProductAnomalies.Should().Contain(x => x.ProductId == anomalous.Id && x.Reasons.Contains("sin_codigo") && x.Reasons.Contains("sin_costo") && x.Reasons.Any(r => r.StartsWith("requiere_revision")));
        report.ProductAnomalies.Should().Contain(x => x.ProductId == negativeStock.Id && x.Reasons.Contains("stock_negativo"));

        report.ProfitableProducts.Should().ContainSingle(x => x.ProductId == products[2].Id && x.QuantitySold == 2m && x.GrossProfit == 90m);
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

    private static SalesController CreateSalesController(ApplicationDbContext db)
    {
        var controller = new SalesController(db)
        {
            ControllerContext = BuildControllerContext()
        };

        return controller;
    }

    private static ReportsController CreateReportsController(ApplicationDbContext db)
    {
        return new ReportsController(db)
        {
            ControllerContext = BuildControllerContext()
        };
    }

    private static ControllerContext BuildControllerContext()
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                ], "TestAuth"))
            }
        };
    }
}
