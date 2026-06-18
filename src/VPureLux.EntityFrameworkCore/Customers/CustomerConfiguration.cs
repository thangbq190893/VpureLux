using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Customers;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "Customers", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(CustomerConsts.MaxCodeLength);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(CustomerConsts.MaxNameLength);
        builder.Property(x => x.CustomerGroupId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PhoneNumber).HasMaxLength(CustomerConsts.MaxPhoneNumberLength);
        builder.Property(x => x.Email).HasMaxLength(CustomerConsts.MaxEmailLength);
        builder.Property(x => x.Address).HasMaxLength(CustomerConsts.MaxAddressLength);
        builder.Property(x => x.TaxCode).HasMaxLength(CustomerConsts.MaxTaxCodeLength);
        builder.Property(x => x.Notes).HasMaxLength(CustomerConsts.MaxNotesLength);

        builder.HasOne<CustomerGroup>()
            .WithMany()
            .HasForeignKey(x => x.CustomerGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Customers_Code");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Customers_Name");

        builder.HasIndex(x => new { x.CustomerGroupId, x.Status })
            .HasDatabaseName("IX_Customers_CustomerGroupId_Status");
    }
}
