using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VPureLux.Catalog;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.EntityFrameworkCore.Catalog;

public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "Components", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(CatalogConsts.MaxCodeLength);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(CatalogConsts.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(CatalogConsts.MaxDescriptionLength);

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(CatalogConsts.MaxUnitLength);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.OwnsOne(x => x.Image, image =>
        {
            image.Property(x => x.ImageBase64)
                .HasColumnName("ImageBase64");
            image.Property(x => x.MimeType)
                .HasColumnName("ImageMimeType")
                .HasMaxLength(CatalogConsts.MaxImageMimeTypeLength);
            image.Property(x => x.FileName)
                .HasColumnName("ImageFileName")
                .HasMaxLength(CatalogConsts.MaxImageFileNameLength);
            image.Property(x => x.ImageHash)
                .HasColumnName("ImageHash")
                .HasColumnType("char(64)")
                .HasMaxLength(CatalogConsts.ImageHashLength);
        });

        builder.Navigation(x => x.Image).IsRequired(false);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("IX_Components_Code");
    }
}
