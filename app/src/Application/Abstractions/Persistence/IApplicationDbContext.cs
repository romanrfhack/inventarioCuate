using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<InventoryBalance> InventoryBalances { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<User> Users { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<InitialInventoryLoad> InitialInventoryLoads { get; }
    DbSet<InitialInventoryLoadDetail> InitialInventoryLoadDetails { get; }
    DbSet<DemoResetAudit> DemoResetAudits { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
