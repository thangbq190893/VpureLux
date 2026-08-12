using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.OperatingCosts;

public class OperatingCostEntryConfiguration : IEntityTypeConfiguration<OperatingCostEntry>
{
    public void Configure(EntityTypeBuilder<OperatingCostEntry> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "OperatingCostEntries", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.EntryDate).IsRequired();
        builder.Property(x => x.Direction).IsRequired();
        builder.Property(x => x.CategoryNameSnapshot)
            .HasMaxLength(OperatingCostConsts.MaxCategoryNameLength)
            .IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentStatus).IsRequired();
        builder.Property(x => x.Counterparty).HasMaxLength(OperatingCostConsts.MaxCounterpartyLength);
        builder.Property(x => x.ReferenceNo).HasMaxLength(OperatingCostConsts.MaxReferenceNoLength);
        builder.Property(x => x.Description).HasMaxLength(OperatingCostConsts.MaxDescriptionLength).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(OperatingCostConsts.MaxNoteLength);

        builder.HasIndex(x => x.EntryDate).HasDatabaseName("IX_OperatingCostEntries_EntryDate");
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("IX_OperatingCostEntries_CategoryId");
        builder.HasIndex(x => new { x.Direction, x.PaymentStatus }).HasDatabaseName("IX_OperatingCostEntries_Direction_PaymentStatus");
        builder.HasOne<OperatingCostCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
