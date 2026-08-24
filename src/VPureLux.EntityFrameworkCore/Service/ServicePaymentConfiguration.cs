using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Service;

public class ServicePaymentConfiguration : IEntityTypeConfiguration<ServicePayment>
{
    public void Configure(EntityTypeBuilder<ServicePayment> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ServicePayments", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Amount).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(ServiceConsts.MaxReferenceNoLength).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(ServiceConsts.MaxNoteLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(ServiceConsts.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Customers.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_ServicePayments_IdempotencyKey");
        builder.HasIndex(x => x.ServiceOrderId).HasDatabaseName("IX_ServicePayments_OrderId");
        builder.HasIndex(x => new { x.CustomerId, x.PaymentDate }).HasDatabaseName("IX_ServicePayments_CustomerId_PaymentDate");
    }
}
