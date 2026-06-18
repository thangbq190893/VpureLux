using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Inventory;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryTransactions", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Type).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(InventoryConsts.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(InventoryConsts.RequestHashLength).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(InventoryConsts.MaxReferenceTypeLength);
        builder.Property(x => x.Reason).HasMaxLength(InventoryConsts.MaxReasonLength).HasColumnType($"nvarchar({InventoryConsts.MaxReasonLength})");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Bom.BomVersion>().WithMany().HasForeignKey(x => x.BomVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_InventoryTransactions_IdempotencyKey");
        builder.HasIndex(x => new { x.WarehouseId, x.PostedAt }).HasDatabaseName("IX_InventoryTransactions_WarehouseId_PostedAt");

        builder.OwnsMany(x => x.Lines, line =>
        {
            line.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryTransactionLines", VPureLuxConsts.DbSchema);
            line.WithOwner().HasForeignKey("InventoryTransactionId");
            line.HasKey(x => x.Id);
            line.Property(x => x.Direction).HasConversion<byte>().IsRequired();
            line.Property(x => x.Quantity).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale).IsRequired();
            line.Property(x => x.LotNo).HasMaxLength(InventoryConsts.MaxLotNoLength);
            line.Property(x => x.UnitCost).HasPrecision(InventoryConsts.CostPrecision, InventoryConsts.CostScale);
            line.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
            line.HasIndex("InventoryTransactionId").HasDatabaseName("IX_InventoryTransactionLines_TransactionId");
            line.OwnsMany(x => x.Allocations, allocation =>
            {
                allocation.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryLotAllocations", VPureLuxConsts.DbSchema);
                allocation.WithOwner().HasForeignKey("InventoryTransactionLineId");
                allocation.HasKey(x => x.Id);
                allocation.Property(x => x.Quantity).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale).IsRequired();
                allocation.Property(x => x.UnitCost).HasPrecision(InventoryConsts.CostPrecision, InventoryConsts.CostScale).IsRequired();
                allocation.HasOne<InventoryLot>().WithMany().HasForeignKey(x => x.InventoryLotId).OnDelete(DeleteBehavior.Restrict);
            });
        });
    }
}
