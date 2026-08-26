using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Sales;

public class SalesOrderCancellationConfiguration : IEntityTypeConfiguration<SalesOrderCancellation>
{
    public void Configure(EntityTypeBuilder<SalesOrderCancellation> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderCancellations", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.ReasonGroup).HasMaxLength(SalesConsts.MaxReasonLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(SalesConsts.MaxReasonLength).IsRequired();
        builder.Property(x => x.StockExceptionReason).HasMaxLength(SalesConsts.MaxReasonLength);
        builder.Property(x => x.StockStatus).HasConversion<byte>().IsRequired();
        builder.Property(x => x.PaymentStatus).HasConversion<byte>().IsRequired();
        builder.Property(x => x.RefundDue).HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
        builder.Property(x => x.RefundedAmount).HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.StockReversalTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SalesOrderId).IsUnique();
    }
}
