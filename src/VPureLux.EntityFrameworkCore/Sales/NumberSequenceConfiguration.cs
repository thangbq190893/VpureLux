using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VPureLux.Sales;

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "NumberSequences", VPureLuxConsts.DbSchema);
        builder.HasKey(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CurrentValue).IsRequired();
    }
}
