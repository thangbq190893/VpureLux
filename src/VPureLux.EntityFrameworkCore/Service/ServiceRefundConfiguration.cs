using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Service;

public class ServiceRefundConfiguration : IEntityTypeConfiguration<ServiceRefund>
{
    public void Configure(EntityTypeBuilder<ServiceRefund> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ServiceRefunds", VPureLuxConsts.DbSchema,
            table => table.HasCheckConstraint("CK_ServiceRefunds_PositiveAmount", "[Amount] > 0"));
        builder.ConfigureByConvention();
        builder.Property(x => x.Amount).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale);
        builder.Property(x => x.Method).HasConversion<byte>();
        builder.Property(x => x.ReferenceNo).HasMaxLength(ServiceConsts.MaxReferenceNoLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(ServiceConsts.MaxNoteLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(ServiceConsts.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Customers.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_ServiceRefunds_IdempotencyKey");
        builder.HasIndex(x => new { x.ServiceOrderId, x.RefundDate, x.CreationTime, x.Id })
            .HasDatabaseName("IX_ServiceRefunds_Order_History");
    }
}
