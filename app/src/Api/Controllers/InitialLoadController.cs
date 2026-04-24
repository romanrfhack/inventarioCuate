using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/initial-load")]
public sealed class InitialLoadController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        var load = new InitialInventoryLoad
        {
            LoadType = "csv_preview_stub",
            FileName = "inventario_inicial_template.csv",
            Status = "previewed",
            UserId = userId,
            ConfirmationToken = "CONFIRM-INITIAL-LOAD",
            SummaryJson = JsonSerializer.Serialize(new
            {
                rows = 2,
                valid = 2,
                warnings = 1,
                message = "Scaffold listo para conectar parser CSV real en el siguiente slice"
            }),
            Details = new List<InitialInventoryLoadDetail>
            {
                new() { SourceRow = 1, Code = "750100000001", Description = "Balata delantera sedan", InitialStock = 6, Cost = 220, SalePrice = 350, RowStatus = "preview_ok" },
                new() { SourceRow = 2, Code = "750100000099", Description = "Producto pendiente de homologación", InitialStock = 1, Cost = 10, SalePrice = 15, RowStatus = "preview_warning", ReviewReason = "No existe match automático" }
            }
        };

        await dbContext.InitialInventoryLoads.AddAsync(load, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { loadId = load.Id, confirmationToken = load.ConfirmationToken, summary = JsonDocument.Parse(load.SummaryJson).RootElement });
    }

    [HttpPost("apply/{loadId:guid}")]
    public async Task<IActionResult> Apply(Guid loadId, [FromQuery] string confirmationToken, CancellationToken cancellationToken)
    {
        var load = await dbContext.InitialInventoryLoads
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == loadId, cancellationToken);

        if (load is null)
        {
            return NotFound();
        }

        if (!string.Equals(load.ConfirmationToken, confirmationToken, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Token de confirmación inválido" });
        }

        load.Status = "ready_for_apply";
        load.SummaryJson = JsonSerializer.Serialize(new
        {
            previous = JsonDocument.Parse(load.SummaryJson).RootElement,
            applied = false,
            nextStep = "Conectar parser y transacción real de carga inicial"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Accepted(new { message = "Carga marcada para aplicación controlada", loadId = load.Id });
    }
}
