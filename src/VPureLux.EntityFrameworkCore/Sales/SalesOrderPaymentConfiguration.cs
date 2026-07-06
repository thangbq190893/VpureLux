using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Sales;

public class SalesOrderPaymentConfiguration : IEntityTypeConfiguration<SalesOrderPayment>
{
    public const string IdempotencyKeyUniqueIndexName = "UX_SalesOrderPayments_IdempotencyKey";

    public void Configure(EntityTypeBuilder<SalesOrderPayment> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderPayments", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Amount)
            .HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale)
            .IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(SalesConsts.MaxPaymentReferenceNoLength).IsRequired();
        builder.Property(x => x.Note).HasColumnType($"nvarchar({SalesConsts.MaxPaymentNoteLength})");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(SalesConsts.MaxIdempotencyKeyLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Customers.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SalesOrderId).HasDatabaseName("IX_SalesOrderPayments_SalesOrderId");
        builder.HasIndex(x => new { x.CustomerId, x.PaymentDate }).HasDatabaseName("IX_SalesOrderPayments_CustomerId_PaymentDate");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName(IdempotencyKeyUniqueIndexName);
    }
}
