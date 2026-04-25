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

public sealed class InitialLoadHttpFlowTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public InitialLoadHttpFlowTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_Then_Apply_Should_Persist_Load_Inventory_And_Movements()
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
        token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string csv = "codigo,descripcion,marca,proveedor,costo,precio_venta,existencia_inicial,unidad,ubicacion,observaciones\nHTTP-001,Producto HTTP 1,Marca Test,Proveedor Test,10.00,15.00,4,pieza,A1,ok\n,Producto HTTP 2,Marca Test,,8.00,,2,pieza,A2,warning\n";

        var previewResponse = await client.PostAsJsonAsync("/api/initial-load/preview", new
        {
            fileName = "http-flow.csv",
            csvContent = csv
        });

        previewResponse.EnsureSuccessStatusCode();
        var previewJson = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var loadId = previewJson.RootElement.GetProperty("loadId").GetGuid();
        var confirmationToken = previewJson.RootElement.GetProperty("confirmationToken").GetString();
        var status = previewJson.RootElement.GetProperty("status").GetString();

        status.Should().Be("previewed");
        confirmationToken.Should().NotBeNullOrWhiteSpace();

        var applyResponse = await client.PostAsJsonAsync($"/api/initial-load/apply/{loadId}", new
        {
            confirmationToken
        });

        applyResponse.EnsureSuccessStatusCode();
        var applyJson = JsonDocument.Parse(await applyResponse.Content.ReadAsStringAsync());
        applyJson.RootElement.GetProperty("status").GetString().Should().Be("applied");

        var loadsResponse = await client.GetAsync("/api/initial-load");
        loadsResponse.EnsureSuccessStatusCode();
        var loadsJson = JsonDocument.Parse(await loadsResponse.Content.ReadAsStringAsync());
        loadsJson.RootElement.EnumerateArray().Any(x => x.GetProperty("loadId").GetGuid() == loadId).Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var load = await db.InitialInventoryLoads.FindAsync(loadId);
        load.Should().NotBeNull();
        load!.Status.Should().Be("applied");

        db.Products.Count().Should().BeGreaterThan(0);
        db.InventoryBalances.Count().Should().BeGreaterThan(0);
        db.InventoryMovements.Count(x => x.MovementType == "carga_inicial").Should().BeGreaterThan(0);
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
