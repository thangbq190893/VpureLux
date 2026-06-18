using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Inventory;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "StockItems", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.ItemType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CatalogItemId).IsRequired();
        builder.Property(x => x.CodeSnapshot).HasMaxLength(InventoryConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.NameSnapshot).HasMaxLength(InventoryConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(InventoryConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.HasIndex(x => new { x.ItemType, x.CatalogItemId })
            .IsUnique()
            .HasDatabaseName("UX_StockItems_ItemType_CatalogItemId");
    }
}
