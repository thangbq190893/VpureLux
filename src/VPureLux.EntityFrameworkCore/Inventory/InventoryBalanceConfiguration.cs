using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VPureLux.Inventory;

public class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryBalances", VPureLuxConsts.DbSchema,
            table =>
            {
                table.HasCheckConstraint("CK_InventoryBalances_QuantityOnHand_NonNegative", "[QuantityOnHand] >= 0");
                table.HasCheckConstraint("CK_InventoryBalances_InventoryValue_NonNegative", "[InventoryValue] >= 0");
            });
        builder.HasKey(x => new { x.WarehouseId, x.StockItemId });
        builder.Property(x => x.QuantityOnHand).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale).IsRequired();
        builder.Property(x => x.InventoryValue).HasPrecision(InventoryConsts.CostPrecision, InventoryConsts.CostScale).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WarehouseId, x.StockItemId }).IsUnique()
            .HasDatabaseName("UX_InventoryBalances_WarehouseId_StockItemId");
    }
}
