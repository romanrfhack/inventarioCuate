using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Inventory;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyCollection<InventorySummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var response = await dbContext.InventoryBalances
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new InventorySummaryResponse(x.ProductId, x.Product.Description, x.CurrentStock, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(response);
    }
}
