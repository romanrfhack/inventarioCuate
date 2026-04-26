using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
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

    [Fact]
    public async Task Preview_Then_Apply_Should_Update_Existing_Product_And_Create_New_Product()
    {
        await ResetAndSeedAsync();
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "admin.demo",
            password = "Demo123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson.RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string csv = "codigo,descripcion,marca,costo,precio_sugerido,unidad\n750100000001,Balata delantera sedan,Genérica,240.00,360.00,pz\nPROV-NEW-01,Bomba de gasolina compacta,MotorPro,420.00,620.00,pz\n,Filtro de aceite compacto,FiltroMax,65.00,110.00,pz\n";

        var previewResponse = await client.PostAsJsonAsync("/api/supplier-catalog-imports/preview", new
        {
            supplierName = "Proveedor HTTP",
            fileName = "supplier-http.csv",
            csvContent = csv
        });

        previewResponse.EnsureSuccessStatusCode();
        var previewJson = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var batchId = previewJson.RootElement.GetProperty("batchId").GetGuid();
        var confirmationToken = previewJson.RootElement.GetProperty("confirmationToken").GetString();

        previewJson.RootElement.GetProperty("newProducts").GetInt32().Should().Be(1);
        previewJson.RootElement.GetProperty("matchedProducts").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var applyResponse = await client.PostAsJsonAsync($"/api/supplier-catalog-imports/{batchId}/apply", new
        {
            confirmationToken
        });

        applyResponse.EnsureSuccessStatusCode();
        var applyJson = JsonDocument.Parse(await applyResponse.Content.ReadAsStringAsync());
        applyJson.RootElement.GetProperty("updatedProducts").GetInt32().Should().Be(1);
        applyJson.RootElement.GetProperty("createdProducts").GetInt32().Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Products.Single(x => x.PrimaryCode == "750100000001").CurrentCost.Should().Be(240.00m);
        db.Products.Single(x => x.PrimaryCode == "PROV-NEW-01").Description.Should().Be("Bomba de gasolina compacta");
        db.SupplierCatalogImportBatches.Single(x => x.Id == batchId).Status.Should().Be("applied");
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
