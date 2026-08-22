using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class CustomerAssetConfiguration : IEntityTypeConfiguration<CustomerAsset>
{
    public void Configure(EntityTypeBuilder<CustomerAsset> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "CustomerAssets", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.AssetNo).HasMaxLength(WarrantyConsts.MaxAssetNoLength).IsRequired();
        builder.Property(x => x.OrderNoSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.ProductNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);

        builder.HasIndex(x => x.AssetNo).IsUnique().HasDatabaseName("UX_CustomerAssets_AssetNo");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CustomerAssets_CustomerId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_CustomerAssets_ProductId");
        builder.HasIndex(x => x.SalesOrderId).HasDatabaseName("IX_CustomerAssets_SalesOrderId");
        builder.HasIndex(x => x.SalesOrderLineId).HasDatabaseName("IX_CustomerAssets_SalesOrderLineId");

        builder.HasOne<VPureLux.Customers.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Sales.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
