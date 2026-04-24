using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Catalog;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog")]
public sealed class CatalogController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.InventoryBalance)
            .OrderBy(x => x.Description)
            .Select(x => new ProductResponse(
                x.Id,
                x.InternalKey,
                x.PrimaryCode,
                x.Description,
                x.Brand,
                x.InventoryBalance != null ? x.InventoryBalance.CurrentStock : 0,
                x.CurrentSalePrice,
                x.RequiresReview))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }
}
