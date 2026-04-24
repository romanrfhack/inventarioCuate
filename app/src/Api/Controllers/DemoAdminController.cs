using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RefaccionariaCuate.Api.Configuration;
using RefaccionariaCuate.Api.Contracts.Demo;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Seed;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/demo-admin")]
public sealed class DemoAdminController(
    ApplicationDbContext dbContext,
    DemoSeedService demoSeedService,
    IOptions<DemoOptions> demoOptions,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var response = new
        {
            environment = hostEnvironment.EnvironmentName,
            allowReset = demoOptions.Value.AllowReset,
            productCount = await dbContext.Products.CountAsync(cancellationToken),
            userCount = await dbContext.Users.CountAsync(cancellationToken),
            pendingInitialLoads = await dbContext.InitialInventoryLoads.CountAsync(x => x.Status != "applied", cancellationToken)
        };

        return Ok(response);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        await demoSeedService.EnsureSeededAsync(cancellationToken);
        return Accepted(new { message = "Seed demo ejecutado o ya presente" });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromBody] DemoResetRequest request, CancellationToken cancellationToken)
    {
        if (!demoOptions.Value.AllowReset || !hostEnvironment.IsEnvironment("Demo"))
        {
            return BadRequest(new { message = "Reset deshabilitado fuera de entorno Demo" });
        }

        if (!string.Equals(request.ConfirmationText, "RESET DEMO", StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Confirmación inválida. Debe enviar RESET DEMO exactamente." });
        }

        var userId = User.Claims.FirstOrDefault(x => x.Type.EndsWith("nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value;
        var affectedProducts = await dbContext.Products.CountAsync(cancellationToken);
        var affectedLoads = await dbContext.InitialInventoryLoads.CountAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM [app].[InitialInventoryLoadDetails]; DELETE FROM [app].[InitialInventoryLoads]; DELETE FROM [app].[InventoryMovements]; DELETE FROM [app].[InventoryBalances]; DELETE FROM [app].[Products];", cancellationToken);

        await dbContext.DemoResetAudits.AddAsync(new DemoResetAudit
        {
            ExecutedByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty,
            Environment = hostEnvironment.EnvironmentName,
            Reason = request.Reason,
            SummaryJson = JsonSerializer.Serialize(new { affectedProducts, affectedLoads, request.ReseedAfterReset })
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.ReseedAfterReset)
        {
            await demoSeedService.EnsureSeededAsync(cancellationToken);
        }

        return Accepted(new { message = "Reset demo ejecutado", reloaded = request.ReseedAfterReset });
    }
}
