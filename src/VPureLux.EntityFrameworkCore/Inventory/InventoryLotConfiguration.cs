using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Inventory;

public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryLots", VPureLuxConsts.DbSchema,
            table => table.HasCheckConstraint("CK_InventoryLots_AvailableQuantity_NonNegative", "[AvailableQuantity] >= 0"));
        builder.ConfigureByConvention();
        builder.Property(x => x.LotNo).HasMaxLength(InventoryConsts.MaxLotNoLength).IsRequired();
        builder.Property(x => x.ReceivedQuantity).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale).IsRequired();
        builder.Property(x => x.AvailableQuantity).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale).IsRequired();
        builder.Property(x => x.UnitCost).HasPrecision(InventoryConsts.CostPrecision, InventoryConsts.CostScale).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WarehouseId, x.StockItemId, x.LotNo })
            .IsUnique().HasDatabaseName("UX_InventoryLots_WarehouseId_StockItemId_LotNo");
        builder.HasIndex(x => new { x.WarehouseId, x.StockItemId, x.ReceivedAt, x.CreationTime, x.Id })
            .HasDatabaseName("IX_InventoryLots_FIFO");
    }
}
