using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class ComponentReplacementPolicyConfiguration : IEntityTypeConfiguration<ComponentReplacementPolicy>
{
    public void Configure(EntityTypeBuilder<ComponentReplacementPolicy> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ComponentReplacementPolicies", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.CycleMonths).IsRequired();
        builder.Property(x => x.WarningDaysBeforeDue).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WarrantyConsts.MaxNoteLength);

        builder.HasIndex(x => x.ComponentId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_ComponentReplacementPolicies_ComponentId");

        builder.HasOne<VPureLux.Catalog.Component>()
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
