using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class ProductMachineSettingConfiguration : IEntityTypeConfiguration<ProductMachineSetting>
{
    public void Configure(EntityTypeBuilder<ProductMachineSetting> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ProductMachineSettings", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_ProductMachineSettings_ProductId");
        builder.HasOne<VPureLux.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
