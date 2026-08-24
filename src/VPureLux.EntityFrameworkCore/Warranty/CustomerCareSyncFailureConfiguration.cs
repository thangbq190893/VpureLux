using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Warranty;

public class CustomerCareSyncFailureConfiguration : IEntityTypeConfiguration<CustomerCareSyncFailure>
{
    public void Configure(EntityTypeBuilder<CustomerCareSyncFailure> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "CustomerCareSyncFailures", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(WarrantyConsts.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(WarrantyConsts.MaxErrorCodeLength);
        builder.Property(x => x.ErrorMessage).HasMaxLength(WarrantyConsts.MaxErrorMessageLength).IsRequired();
        builder.Property(x => x.ErrorContext).HasMaxLength(WarrantyConsts.MaxErrorContextLength);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_CustomerCareSyncFailures_IdempotencyKey");
        builder.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("IX_CustomerCareSyncFailures_Status_NextRetryAt");
        builder.HasIndex(x => x.SalesOrderLineId)
            .HasDatabaseName("IX_CustomerCareSyncFailures_SalesOrderLineId");

        builder.HasOne<VPureLux.Sales.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CustomerCareSyncFailures_AttemptCount",
            "[AttemptCount] > 0"));
    }
}
