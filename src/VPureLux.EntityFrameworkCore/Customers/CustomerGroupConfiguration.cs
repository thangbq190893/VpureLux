using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Customers;

public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "CustomerGroups", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(CustomerGroupConsts.MaxCodeLength);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(CustomerGroupConsts.MaxNameLength);
        builder.Property(x => x.Description).HasMaxLength(CustomerGroupConsts.MaxDescriptionLength);
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_CustomerGroups_Code");

        builder.HasIndex(x => new { x.Status, x.SortOrder })
            .HasDatabaseName("IX_CustomerGroups_Status_SortOrder");
    }
}
