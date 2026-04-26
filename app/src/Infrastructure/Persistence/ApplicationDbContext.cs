using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Application.Abstractions.Persistence;
using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<InitialInventoryLoad> InitialInventoryLoads => Set<InitialInventoryLoad>();
    public DbSet<InitialInventoryLoadDetail> InitialInventoryLoadDetails => Set<InitialInventoryLoadDetail>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<DemoResetAudit> DemoResetAudits => Set<DemoResetAudit>();
    public DbSet<SupplierCatalogImportBatch> SupplierCatalogImportBatches => Set<SupplierCatalogImportBatch>();
    public DbSet<SupplierCatalogImportDetail> SupplierCatalogImportDetails => Set<SupplierCatalogImportDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasIndex(x => x.InternalKey).IsUnique();
            entity.Property(x => x.InternalKey).HasMaxLength(64);
            entity.Property(x => x.PrimaryCode).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.Property(x => x.Brand).HasMaxLength(128);
            entity.Property(x => x.CurrentCost).HasPrecision(18, 2);
            entity.Property(x => x.CurrentSalePrice).HasPrecision(18, 2);
            entity.Property(x => x.Unit).HasMaxLength(16);
            entity.Property(x => x.PiecesPerBox).HasPrecision(18, 2);
            entity.Property(x => x.Compatibility).HasMaxLength(512);
            entity.Property(x => x.Line).HasMaxLength(128);
            entity.Property(x => x.Family).HasMaxLength(128);
            entity.Property(x => x.SubFamily).HasMaxLength(128);
            entity.Property(x => x.Category).HasMaxLength(128);
            entity.Property(x => x.SupplierName).HasMaxLength(128);
            entity.Property(x => x.SupplierAvailability).HasPrecision(18, 2);
            entity.Property(x => x.SupplierStockText).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.InventoryBalance)
                .WithOne(x => x.Product)
                .HasForeignKey<InventoryBalance>(x => x.ProductId);
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("InventoryBalances");
            entity.HasIndex(x => x.ProductId).IsUnique();
            entity.Property(x => x.CurrentStock).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Location).HasMaxLength(128);
            entity.Property(x => x.BaseOrigin).HasMaxLength(64);
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.ToTable("InventoryMovements");
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt });
            entity.Property(x => x.MovementType).HasMaxLength(64);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ResultingStock).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceType).HasMaxLength(64);
            entity.Property(x => x.SourceId).HasMaxLength(128);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.Property(x => x.UserName).HasMaxLength(64);
            entity.Property(x => x.FullName).HasMaxLength(128);
            entity.Property(x => x.Role).HasMaxLength(32);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("Shifts");
            entity.HasIndex(x => new { x.UserId, x.OpenedAt });
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(512);
        });

        modelBuilder.Entity<InitialInventoryLoad>(entity =>
        {
            entity.ToTable("InitialInventoryLoads");
            entity.Property(x => x.LoadType).HasMaxLength(64);
            entity.Property(x => x.FileName).HasMaxLength(256);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ConfirmationToken).HasMaxLength(64);
            entity.HasMany(x => x.Details)
                .WithOne(x => x.InitialInventoryLoad)
                .HasForeignKey(x => x.InitialInventoryLoadId);
        });

        modelBuilder.Entity<InitialInventoryLoadDetail>(entity =>
        {
            entity.ToTable("InitialInventoryLoadDetails");
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.Property(x => x.Brand).HasMaxLength(128);
            entity.Property(x => x.Supplier).HasMaxLength(128);
            entity.Property(x => x.Unit).HasMaxLength(32);
            entity.Property(x => x.Location).HasMaxLength(128);
            entity.Property(x => x.Notes).HasMaxLength(512);
            entity.Property(x => x.RowStatus).HasMaxLength(32);
            entity.Property(x => x.InitialStock).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasIndex(x => x.Folio).IsUnique();
            entity.Property(x => x.Folio).HasMaxLength(32);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)");
            entity.HasMany(x => x.Details)
                .WithOne(x => x.Sale)
                .HasForeignKey(x => x.SaleId);
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.ToTable("SaleDetails");
            entity.HasIndex(x => x.SaleId);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Product)
                .WithMany(x => x.SaleDetails)
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<DemoResetAudit>(entity =>
        {
            entity.ToTable("DemoResetAudits");
            entity.Property(x => x.Environment).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(256);
        });

        modelBuilder.Entity<SupplierCatalogImportBatch>(entity =>
        {
            entity.ToTable("SupplierCatalogImportBatches");
            entity.Property(x => x.SupplierName).HasMaxLength(128);
            entity.Property(x => x.ImportProfile).HasMaxLength(64);
            entity.Property(x => x.FileName).HasMaxLength(256);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ConfirmationToken).HasMaxLength(64);
            entity.HasMany(x => x.Details)
                .WithOne(x => x.SupplierCatalogImportBatch)
                .HasForeignKey(x => x.SupplierCatalogImportBatchId);
        });

        modelBuilder.Entity<SupplierCatalogImportDetail>(entity =>
        {
            entity.ToTable("SupplierCatalogImportDetails");
            entity.Property(x => x.SourceSheet).HasMaxLength(128);
            entity.Property(x => x.SupplierName).HasMaxLength(128);
            entity.Property(x => x.ImportProfile).HasMaxLength(64);
            entity.Property(x => x.SupplierProductCode).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.Property(x => x.Brand).HasMaxLength(128);
            entity.Property(x => x.Unit).HasMaxLength(32);
            entity.Property(x => x.PiecesPerBox).HasPrecision(18, 2);
            entity.Property(x => x.Compatibility).HasMaxLength(512);
            entity.Property(x => x.Line).HasMaxLength(128);
            entity.Property(x => x.Family).HasMaxLength(128);
            entity.Property(x => x.SubFamily).HasMaxLength(128);
            entity.Property(x => x.Category).HasMaxLength(128);
            entity.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SuggestedSalePrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PriceLevelsJson).HasColumnType("TEXT");
            entity.Property(x => x.SupplierAvailability).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SupplierStockText).HasMaxLength(128);
            entity.Property(x => x.RevisionReason).HasMaxLength(512);
            entity.Property(x => x.MatchType).HasMaxLength(64);
            entity.Property(x => x.ActionType).HasMaxLength(32);
            entity.Property(x => x.RowStatus).HasMaxLength(32);
            entity.Property(x => x.ReviewReason).HasMaxLength(512);
            entity.Property(x => x.ProposedCost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ProposedSalePrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.SupplierCatalogImportBatchId, x.SourceRow }).IsUnique();
        });
    }
}
