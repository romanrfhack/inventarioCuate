using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Domain.Enums;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Services;

namespace RefaccionariaCuate.Infrastructure.Seed;

public sealed class DemoSeedService(ApplicationDbContext dbContext, IConfiguration configuration)
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var admin = await dbContext.Users.SingleOrDefaultAsync(x => x.UserName == "admin.demo", cancellationToken);
        if (admin is null)
        {
            admin = new User
            {
                UserName = "admin.demo",
                FullName = "Administrador Demo",
                Role = UserRole.Admin,
                PasswordHash = PasswordHasher.Hash(configuration["Demo:DefaultAdminPassword"] ?? "Demo123!")
            };
            await dbContext.Users.AddAsync(admin, cancellationToken);
        }

        var operatorUser = await dbContext.Users.SingleOrDefaultAsync(x => x.UserName == "operador.demo", cancellationToken);
        if (operatorUser is null)
        {
            operatorUser = new User
            {
                UserName = "operador.demo",
                FullName = "Operador Demo",
                Role = UserRole.Operador,
                PasswordHash = PasswordHasher.Hash("Demo123!")
            };
            await dbContext.Users.AddAsync(operatorUser, cancellationToken);
        }

        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var products = new[]
        {
            new Product { InternalKey = "BAL-001", PrimaryCode = "750100000001", Description = "Balata delantera sedan", Brand = "Genérica", CurrentCost = 220, CurrentSalePrice = 350, Unit = "pz" },
            new Product { InternalKey = "ACE-5W30", PrimaryCode = "750100000002", Description = "Aceite 5W30 litro", Brand = "MotorPro", CurrentCost = 95, CurrentSalePrice = 145, Unit = "lt" },
            new Product { InternalKey = "FIL-015", PrimaryCode = "750100000003", Description = "Filtro de aceite compacto", Brand = "FiltroMax", CurrentCost = 65, CurrentSalePrice = 110, Unit = "pz" }
        };

        foreach (var product in products)
        {
            product.InventoryBalance = new InventoryBalance
            {
                ProductId = product.Id,
                CurrentStock = product.InternalKey == "ACE-5W30" ? 18 : 10,
                BaseOrigin = "seed_demo",
                BaseCutDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            product.Movements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = MovementType.CargaInicial,
                Quantity = product.InventoryBalance.CurrentStock,
                ResultingStock = product.InventoryBalance.CurrentStock,
                SourceType = "seed_demo",
                SourceId = product.InternalKey,
                Reason = "Carga demo automática"
            });
        }

        var initialLoad = new InitialInventoryLoad
        {
            LoadType = "demo_seed",
            FileName = "seed-demo-interno",
            Status = "applied",
            SummaryJson = JsonSerializer.Serialize(new { rows = 3, source = "internal_seed" }),
            UserId = admin.Id,
            ConfirmationToken = "SEED-DEMO-0001",
            Details = products.Select((product, index) => new InitialInventoryLoadDetail
            {
                SourceRow = index + 1,
                Code = product.PrimaryCode,
                Description = product.Description,
                InitialStock = product.InventoryBalance!.CurrentStock,
                Cost = product.CurrentCost,
                SalePrice = product.CurrentSalePrice,
                MatchedProductId = product.Id,
                RowStatus = "applied"
            }).ToList()
        };

        await dbContext.Products.AddRangeAsync(products, cancellationToken);
        await dbContext.InitialInventoryLoads.AddAsync(initialLoad, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
