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
        builder.Property(x => x.Source).HasConversion<byte?>();
        builder.Property(x => x.OrderNoSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength);
        builder.Property(x => x.CustomerCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength);
        builder.Property(x => x.ProductNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength);
        builder.Property(x => x.SerialNo).HasMaxLength(WarrantyConsts.MaxSerialNoLength);
        builder.Property(x => x.Brand).HasMaxLength(WarrantyConsts.MaxBrandLength);
        builder.Property(x => x.Model).HasMaxLength(WarrantyConsts.MaxModelLength);
        builder.Property(x => x.ExternalReference).HasMaxLength(WarrantyConsts.MaxCodeLength);
        builder.Property(x => x.InstallationAddress).HasMaxLength(WarrantyConsts.MaxAddressLength);
        builder.Property(x => x.InstallationIdempotencyKey).HasMaxLength(WarrantyConsts.MaxIdempotencyKeyLength);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.AssetNo).IsUnique().HasDatabaseName("UX_CustomerAssets_AssetNo");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CustomerAssets_CustomerId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_CustomerAssets_ProductId");
        builder.HasIndex(x => x.SalesOrderId).HasDatabaseName("IX_CustomerAssets_SalesOrderId");
        builder.HasIndex(x => x.SalesOrderLineId).HasDatabaseName("IX_CustomerAssets_SalesOrderLineId");
        builder.HasIndex(x => new { x.SalesOrderLineId, x.SourceUnitIndex })
            .IsUnique()
            .HasFilter("[SalesOrderLineId] IS NOT NULL AND [SourceUnitIndex] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_CustomerAssets_SourceLine_UnitIndex");
        builder.HasIndex(x => x.SerialNo)
            .HasFilter("[SerialNo] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_CustomerAssets_SerialNo_Review");
        builder.HasIndex(x => x.InstallationIdempotencyKey)
            .IsUnique()
            .HasFilter("[InstallationIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_CustomerAssets_InstallationIdempotencyKey");

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

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CustomerAssets_SourceReferences",
            "[Source] IS NULL OR [Source] = 2 OR ([ProductId] IS NOT NULL AND [SalesOrderId] IS NOT NULL AND [SalesOrderLineId] IS NOT NULL AND [SourceUnitIndex] > 0)"));
    }
}
