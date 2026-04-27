using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using Xunit;

namespace RefaccionariaCuate.IntegrationTests;

public sealed class SupplierCatalogImportHttpFlowTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public SupplierCatalogImportHttpFlowTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("alessia", "Alessia", "alessia/alessia-fixture.xlsx")]
    [InlineData("masuda", "Masuda", "masuda/masuda-fixture.xlsx")]
    [InlineData("c-cedis", "C-CEDIS", "c-cedis/c-cedis-fixture.xlsx")]
    public async Task Preview_Should_Parse_Known_Provider_Files(string profile, string supplierName, string relativeFixturePath)
    {
        await ResetAndSeedAsync();
        var client = await CreateAuthorizedClientAsync();

        var filePath = GetFixturePath(relativeFixturePath);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(supplierName), "supplierName");
        form.Add(new StringContent(profile), "importProfile");
        form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));

        var response = await client.PostAsync("/api/provider-catalogs/preview", form);
        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("totalRows").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("importProfile").GetString().Should().Be(profile);
    }

    [Fact]
    public async Task Preview_Then_Apply_Should_Update_Catalog_Without_Modifying_Local_Inventory()
    {
        await ResetAndSeedAsync();
        var client = await CreateAuthorizedClientAsync();

        Guid productId;
        decimal stockBefore;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seededDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var productWithInventory = seededDb.Products.Include(x => x.InventoryBalance).First(x => x.InventoryBalance != null);
            productId = productWithInventory.Id;
            stockBefore = productWithInventory.InventoryBalance!.CurrentStock;
        }

        var samplePath = GetFixturePath("alessia/alessia-fixture.xlsx");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Alessia"), "supplierName");
        form.Add(new StringContent("alessia"), "importProfile");
        form.Add(new StreamContent(File.OpenRead(samplePath)), "file", Path.GetFileName(samplePath));

        var previewResponse = await client.PostAsync("/api/provider-catalogs/preview", form);
        previewResponse.EnsureSuccessStatusCode();
        var previewJson = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var batchId = previewJson.RootElement.GetProperty("batchId").GetGuid();
        var confirmationToken = previewJson.RootElement.GetProperty("confirmationToken").GetString();

        var applyResponse = await client.PostAsJsonAsync($"/api/provider-catalogs/apply/{batchId}", new { confirmationToken });
        applyResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedProduct = db.Products.Include(x => x.InventoryBalance).Single(x => x.Id == productId);
        persistedProduct.InventoryBalance!.CurrentStock.Should().Be(stockBefore);
        db.SupplierCatalogImportBatches.Single(x => x.Id == batchId).Status.Should().NotBeNullOrWhiteSpace();
        db.ProductSupplierCatalogSnapshots.Should().Contain(x => x.LastImportBatchId == batchId);
    }

    [Fact]
    public async Task Apply_Should_Keep_Independent_Snapshots_Per_Supplier_For_Same_Product()
    {
        await ResetAndSeedAsync();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = seedDb.Products.First();
            product.PrimaryCode = "DUP-001";
            product.Description = "Producto compartido";
            await seedDb.SaveChangesAsync();
        }

        var client = await CreateAuthorizedClientAsync();

        var firstBatchId = await PreviewAndApplyAsync(client, "Alessia", "alessia", GetFixturePath("alessia/alessia-fixture.xlsx"));
        var secondBatchId = await PreviewAndApplyAsync(client, "Masuda", "masuda", GetFixturePath("masuda/masuda-fixture.xlsx"));

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var snapshots = await assertDb.ProductSupplierCatalogSnapshots
            .AsNoTracking()
            .Where(x => x.SupplierCode == "DUP-001")
            .OrderBy(x => x.SupplierProfile)
            .ToListAsync();

        snapshots.Should().HaveCount(2);
        snapshots.Select(x => x.SupplierProfile).Should().BeEquivalentTo(["alessia", "masuda"]);
        snapshots.Select(x => x.LastImportBatchId).Should().Contain([firstBatchId, secondBatchId]);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { userName = "admin.demo", password = "Demo123!" });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginJson.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private async Task<Guid> PreviewAndApplyAsync(HttpClient client, string supplierName, string importProfile, string filePath)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(supplierName), "supplierName");
        form.Add(new StringContent(importProfile), "importProfile");
        form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));

        var previewResponse = await client.PostAsync("/api/provider-catalogs/preview", form);
        previewResponse.EnsureSuccessStatusCode();
        var previewJson = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var batchId = previewJson.RootElement.GetProperty("batchId").GetGuid();
        var confirmationToken = previewJson.RootElement.GetProperty("confirmationToken").GetString();

        var applyResponse = await client.PostAsJsonAsync($"/api/provider-catalogs/apply/{batchId}", new { confirmationToken });
        applyResponse.EnsureSuccessStatusCode();
        return batchId;
    }

    private static string GetFixturePath(string relativeFixturePath)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../data/provider-catalogs/fixtures", relativeFixturePath));
    }

    private async Task ResetAndSeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        var seed = scope.ServiceProvider.GetRequiredService<DemoSeedService>();
        await seed.EnsureSeededAsync();
    }
}
