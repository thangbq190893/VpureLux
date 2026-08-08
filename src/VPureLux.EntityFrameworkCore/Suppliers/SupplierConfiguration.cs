using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Suppliers;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "Suppliers", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Code).HasMaxLength(SupplierConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(SupplierConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ContactName).HasMaxLength(SupplierConsts.MaxContactNameLength);
        builder.Property(x => x.Phone).HasMaxLength(SupplierConsts.MaxPhoneLength);
        builder.Property(x => x.Email).HasMaxLength(SupplierConsts.MaxEmailLength);
        builder.Property(x => x.TaxCode).HasMaxLength(SupplierConsts.MaxTaxCodeLength);
        builder.Property(x => x.Address).HasMaxLength(SupplierConsts.MaxAddressLength);
        builder.Property(x => x.Note).HasMaxLength(SupplierConsts.MaxNoteLength);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Suppliers_Code");
    }
}
