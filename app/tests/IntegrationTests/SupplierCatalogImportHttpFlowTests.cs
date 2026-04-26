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
    [InlineData("alessia", "Alessia", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/alessia/07 Abril Lista Alessia 26.xlsm")]
    [InlineData("masuda", "Masuda", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/masuda/LISTA DE PRECIO - MASUDA IMPORTADOR REGIONAL 09-ABRIL.xlsx")]
    [InlineData("c-cedis", "C-CEDIS", "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/c-cedis/ListaPreciosC-CEDIS-05042026.xlsx.xls")]
    public async Task Preview_Should_Parse_Known_Provider_Files(string profile, string supplierName, string filePath)
    {
        await ResetAndSeedAsync();
        var client = await CreateAuthorizedClientAsync();

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

        const string samplePath = "/root/projects/refaccionaria-cuate/data/provider-catalogs/raw/alessia/07 Abril Lista Alessia 26.xlsm";
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
