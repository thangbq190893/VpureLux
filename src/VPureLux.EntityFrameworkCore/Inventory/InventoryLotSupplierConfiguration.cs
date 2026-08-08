using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Inventory;

public class InventoryLotSupplierConfiguration : IEntityTypeConfiguration<InventoryLotSupplier>
{
    public void Configure(EntityTypeBuilder<InventoryLotSupplier> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "InventoryLotSuppliers", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.SupplierCodeSnapshot).HasMaxLength(Suppliers.SupplierConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.SupplierNameSnapshot).HasMaxLength(Suppliers.SupplierConsts.MaxNameLength).IsRequired();
        builder.HasOne<InventoryLot>().WithMany().HasForeignKey(x => x.InventoryLotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Suppliers.Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.InventoryLotId).IsUnique().HasDatabaseName("UX_InventoryLotSuppliers_InventoryLotId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_InventoryLotSuppliers_SupplierId");
    }
}
