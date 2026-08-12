using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.OperatingCosts;

public class OperatingCostCategoryConfiguration : IEntityTypeConfiguration<OperatingCostCategory>
{
    public void Configure(EntityTypeBuilder<OperatingCostCategory> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "OperatingCostCategories", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Code).HasMaxLength(OperatingCostConsts.MaxCategoryCodeLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(OperatingCostConsts.MaxCategoryNameLength).IsRequired();
        builder.Property(x => x.Direction).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_OperatingCostCategories_Code");
    }
}
