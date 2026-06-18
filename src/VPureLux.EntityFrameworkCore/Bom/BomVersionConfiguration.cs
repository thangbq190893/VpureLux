using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VPureLux.Catalog;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Bom;

public class BomVersionConfiguration : IEntityTypeConfiguration<BomVersion>
{
    public const string PublishedProductUniqueIndexName = "UX_BomVersions_ProductId_Published";

    public void Configure(EntityTypeBuilder<BomVersion> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "BomVersions", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.VersionNo)
            .HasConversion(
                versionNo => versionNo.Value,
                value => new BomVersionNo(value))
            .HasColumnName(nameof(BomVersion.VersionNo))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductId, x.VersionNo })
            .IsUnique()
            .HasDatabaseName("IX_BomVersions_ProductId_VersionNo");

        builder.HasIndex(x => new { x.ProductId, x.Status })
            .HasDatabaseName("IX_BomVersions_ProductId_Status");

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[Status] = 2 AND [IsDeleted] = 0")
            .HasDatabaseName(PublishedProductUniqueIndexName);

        builder.OwnsMany(x => x.Items, itemBuilder =>
        {
            itemBuilder.ToTable(VPureLuxConsts.DbTablePrefix + "BomItems", VPureLuxConsts.DbSchema);
            itemBuilder.WithOwner()
                .HasForeignKey("BomVersionId");

            itemBuilder.HasKey(x => x.Id);

            itemBuilder.Property(x => x.ComponentId)
                .IsRequired();

            itemBuilder.Property(x => x.Quantity)
                .HasPrecision(BomConsts.QuantityPrecision, BomConsts.QuantityScale)
                .IsRequired();

            itemBuilder.HasOne<Component>()
                .WithMany()
                .HasForeignKey(x => x.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            itemBuilder.HasIndex("BomVersionId")
                .HasDatabaseName("IX_BomItems_BomVersionId");
        });
    }
}
