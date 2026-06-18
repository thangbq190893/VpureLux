using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VPureLux.Catalog;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Pricing;

public class ComponentSuggestedSellingPriceVersionConfiguration
    : IEntityTypeConfiguration<ComponentSuggestedSellingPriceVersion>
{
    public const string ActiveComponentUniqueIndexName =
        "UX_ComponentSuggestedSellingPriceVersions_ComponentId_Active";
    public const string ComponentVersionUniqueIndexName =
        "UX_ComponentSuggestedSellingPriceVersions_ComponentId_VersionNo";

    public void Configure(EntityTypeBuilder<ComponentSuggestedSellingPriceVersion> builder)
    {
        builder.ToTable(
            VPureLuxConsts.DbTablePrefix + "ComponentSuggestedSellingPriceVersions",
            VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.ComponentId).IsRequired();
        builder.Property(x => x.VersionNo)
            .HasConversion(versionNo => versionNo.Value, value => new PriceVersionNo(value))
            .HasColumnName(nameof(ComponentSuggestedSellingPriceVersion.VersionNo))
            .IsRequired();
        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(PricingConsts.MaxReasonLength)
            .HasColumnType($"nvarchar({PricingConsts.MaxReasonLength})");
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();

        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(x => x.Amount)
                .HasColumnName("Price")
                .HasPrecision(PricingConsts.PricePrecision, PricingConsts.PriceScale)
                .IsRequired();
            price.Property(x => x.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.EffectivePeriod, period =>
        {
            period.Property(x => x.EffectiveFrom)
                .HasColumnName("EffectiveFrom")
                .IsRequired();
            period.Property(x => x.EffectiveTo)
                .HasColumnName("EffectiveTo");
        });

        builder.HasOne<Component>()
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ComponentId, x.VersionNo })
            .IsUnique()
            .HasDatabaseName(ComponentVersionUniqueIndexName);

        builder.HasIndex(x => x.ComponentId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName(ActiveComponentUniqueIndexName);
    }
}
