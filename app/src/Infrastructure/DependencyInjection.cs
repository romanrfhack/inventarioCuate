using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefaccionariaCuate.Application.Abstractions.Auth;
using RefaccionariaCuate.Infrastructure.Authentication;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;
using RefaccionariaCuate.Infrastructure.Services;

namespace RefaccionariaCuate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=refaccionaria-cuate.db";
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseSqlite(connectionString)
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
        }

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<DemoSeedService>();
        services.AddScoped<InitialInventoryCsvParser>();
        services.AddScoped<SupplierCatalogSpreadsheetParser>();
        services.AddScoped<SupplierCatalogMatcher>();

        return services;
    }
}
