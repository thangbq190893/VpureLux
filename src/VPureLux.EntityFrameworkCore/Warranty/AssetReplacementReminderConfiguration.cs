using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class AssetReplacementReminderConfiguration : IEntityTypeConfiguration<AssetReplacementReminder>
{
    public void Configure(EntityTypeBuilder<AssetReplacementReminder> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "AssetReplacementReminders", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.ComponentCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.ComponentNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ComponentUnitSnapshot).HasMaxLength(WarrantyConsts.MaxUnitLength).IsRequired();
        builder.Property(x => x.QuantityPerProductSnapshot)
            .HasPrecision(WarrantyConsts.MaxQuantityPrecision, WarrantyConsts.MaxQuantityScale)
            .IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.TriggerSource).HasConversion<byte?>();
        builder.Property(x => x.SourceReferenceType).HasMaxLength(WarrantyConsts.MaxSourceTypeLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(WarrantyConsts.MaxIdempotencyKeyLength);
        builder.Property(x => x.CloseReason).HasMaxLength(WarrantyConsts.MaxCloseReasonLength);
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.Status, x.DueDate }).HasDatabaseName("IX_AssetReplacementReminders_Status_DueDate");
        builder.HasIndex(x => x.CustomerAssetId).HasDatabaseName("IX_AssetReplacementReminders_CustomerAssetId");
        builder.HasIndex(x => x.ComponentId).HasDatabaseName("IX_AssetReplacementReminders_ComponentId");
        builder.HasIndex(x => x.SalesOrderId).HasDatabaseName("IX_AssetReplacementReminders_SalesOrderId");
        builder.HasIndex(x => x.SalesOrderLineId).HasDatabaseName("IX_AssetReplacementReminders_SalesOrderLineId");
        builder.HasIndex(x => x.CustomerAssetComponentId).HasDatabaseName("IX_AssetReplacementReminders_AssetComponentId");
        builder.HasIndex(x => new { x.Status, x.WarningDate }).HasDatabaseName("IX_AssetReplacementReminders_Status_WarningDate");
        builder.HasIndex(x => x.CustomerAssetComponentId)
            .IsUnique()
            .HasFilter("[CustomerAssetComponentId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_AssetReplacementReminders_OpenPosition");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_AssetReplacementReminders_IdempotencyKey");

        builder.HasOne<CustomerAsset>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Catalog.Component>()
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerAssetComponent>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAssetComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Sales.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
