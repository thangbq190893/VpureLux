using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class AssetMaintenanceEventConfiguration : IEntityTypeConfiguration<AssetMaintenanceEvent>
{
    public void Configure(EntityTypeBuilder<AssetMaintenanceEvent> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "AssetMaintenanceEvents", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.EventType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.SourceType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ComponentCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength);
        builder.Property(x => x.ComponentNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(WarrantyConsts.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);

        builder.HasIndex(x => new { x.CustomerAssetId, x.OccurredAt })
            .HasDatabaseName("IX_AssetMaintenanceEvents_Asset_OccurredAt");
        builder.HasIndex(x => x.CustomerAssetComponentId)
            .HasDatabaseName("IX_AssetMaintenanceEvents_AssetComponentId");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_AssetMaintenanceEvents_IdempotencyKey");

        builder.HasOne<CustomerAsset>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerAssetComponent>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAssetComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Catalog.Component>()
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
