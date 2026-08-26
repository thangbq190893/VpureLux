using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Sales;

public class SalesOrderRefundConfiguration : IEntityTypeConfiguration<SalesOrderRefund>
{
    public const string IdempotencyIndexName = "UX_SalesOrderRefunds_IdempotencyKey";

    public void Configure(EntityTypeBuilder<SalesOrderRefund> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderRefunds", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Amount).HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
        builder.Property(x => x.PaymentMethod).HasConversion<byte>();
        builder.Property(x => x.ReferenceNo).HasMaxLength(SalesConsts.MaxPaymentReferenceNoLength);
        builder.Property(x => x.Reason).HasMaxLength(SalesConsts.MaxReasonLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(SalesConsts.MaxIdempotencyKeyLength);
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesOrderRevision>().WithMany().HasForeignKey(x => x.SalesOrderRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesOrderCancellation>().WithMany().HasForeignKey(x => x.SalesOrderCancellationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName(IdempotencyIndexName);
        builder.HasIndex(x => x.SalesOrderId);
    }
}
