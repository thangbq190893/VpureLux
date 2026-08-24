using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class CustomerAssetComponentConfiguration : IEntityTypeConfiguration<CustomerAssetComponent>
{
    public void Configure(EntityTypeBuilder<CustomerAssetComponent> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "CustomerAssetComponents", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.PositionCode).HasMaxLength(WarrantyConsts.MaxPositionCodeLength).IsRequired();
        builder.Property(x => x.PositionName).HasMaxLength(WarrantyConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ComponentCodeSnapshot).HasMaxLength(WarrantyConsts.MaxCodeLength);
        builder.Property(x => x.ComponentNameSnapshot).HasMaxLength(WarrantyConsts.MaxNameLength);
        builder.Property(x => x.ComponentUnitSnapshot).HasMaxLength(WarrantyConsts.MaxUnitLength);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.CustomerAssetId, x.PositionCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_CustomerAssetComponents_ActivePosition");
        builder.HasIndex(x => x.ComponentId).HasDatabaseName("IX_CustomerAssetComponents_ComponentId");
        builder.HasIndex(x => new { x.Status, x.ReplacementBaselineDate })
            .HasDatabaseName("IX_CustomerAssetComponents_Status_BaselineDate");

        builder.HasOne<CustomerAsset>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Catalog.Component>()
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CustomerAssetComponents_Quantity",
            "[Quantity] > 0"));
    }
}
