using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Inventory;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "Warehouses", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Code).HasMaxLength(InventoryConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(InventoryConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(InventoryConsts.MaxAddressLength);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Warehouses_Code");
    }
}
