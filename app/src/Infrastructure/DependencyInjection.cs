using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefaccionariaCuate.Application.Abstractions.Auth;
using RefaccionariaCuate.Infrastructure.Authentication;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;

namespace RefaccionariaCuate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<DemoSeedService>();

        return services;
    }
}
