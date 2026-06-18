using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VPureLux.Catalog;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Pricing;

public class ProductSuggestedPriceVersionConfiguration
    : IEntityTypeConfiguration<ProductSuggestedPriceVersion>
{
    public const string ActiveProductUniqueIndexName =
        "UX_ProductSuggestedPriceVersions_ProductId_Active";
    public const string ProductVersionUniqueIndexName =
        "UX_ProductSuggestedPriceVersions_ProductId_VersionNo";

    public void Configure(EntityTypeBuilder<ProductSuggestedPriceVersion> builder)
    {
        builder.ToTable(
            VPureLuxConsts.DbTablePrefix + "ProductSuggestedPriceVersions",
            VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.VersionNo)
            .HasConversion(versionNo => versionNo.Value, value => new PriceVersionNo(value))
            .HasColumnName(nameof(ProductSuggestedPriceVersion.VersionNo))
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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductId, x.VersionNo })
            .IsUnique()
            .HasDatabaseName(ProductVersionUniqueIndexName);

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName(ActiveProductUniqueIndexName);
    }
}
