using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Extensions;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!app.Environment.IsEnvironment("IntegrationTest"))
    {
        await dbContext.Database.MigrateAsync();
    }

    if (app.Environment.IsEnvironment("Demo"))
    {
        var demoSeedService = scope.ServiceProvider.GetRequiredService<DemoSeedService>();
        await demoSeedService.EnsureSeededAsync();
    }
}

app.Run();

public partial class Program;
