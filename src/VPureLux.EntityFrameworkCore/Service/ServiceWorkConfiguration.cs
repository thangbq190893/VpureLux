using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Service;

public class ServiceWorkConfiguration : IEntityTypeConfiguration<ServiceWork>
{
    public void Configure(EntityTypeBuilder<ServiceWork> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ServiceWorks", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Code).HasMaxLength(ServiceConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(ServiceConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.DefaultPrice).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Note).HasMaxLength(ServiceConsts.MaxNoteLength);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_ServiceWorks_Code");
        builder.HasIndex(x => new { x.Status, x.Name }).HasDatabaseName("IX_ServiceWorks_Status_Name");
    }
}
