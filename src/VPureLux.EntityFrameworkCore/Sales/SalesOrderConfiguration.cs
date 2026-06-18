using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Sales;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public const string OrderNoUniqueIndexName = "UX_SalesOrders_OrderNo";
    public const string ConfirmationKeyUniqueIndexName = "UX_SalesOrders_ConfirmationIdempotencyKey";

    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrders", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.OrderNo).HasMaxLength(SalesConsts.MaxOrderNoLength).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CustomerCodeSnapshot).HasMaxLength(SalesConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(SalesConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.CustomerGroupCodeSnapshot).HasMaxLength(SalesConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerGroupNameSnapshot).HasMaxLength(SalesConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ConfirmationIdempotencyKey).HasMaxLength(SalesConsts.MaxIdempotencyKeyLength);
        ConfigureMoney(builder.Property(x => x.TotalRevenueAmount));
        ConfigureMoney(builder.Property(x => x.TotalCostAmount));
        ConfigureMoney(builder.Property(x => x.TotalProfitAmount));
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<VPureLux.Customers.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Customers.CustomerGroup>().WithMany().HasForeignKey(x => x.CustomerGroupIdSnapshot).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Inventory.Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OrderNo).IsUnique().HasDatabaseName(OrderNoUniqueIndexName);
        builder.HasIndex(x => x.ConfirmationIdempotencyKey).IsUnique()
            .HasFilter("[ConfirmationIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName(ConfirmationKeyUniqueIndexName);
        builder.HasIndex(x => new { x.CustomerId, x.OrderDate }).HasDatabaseName("IX_SalesOrders_CustomerId_OrderDate");
        builder.HasIndex(x => new { x.Status, x.OrderDate }).HasDatabaseName("IX_SalesOrders_Status_OrderDate");

        builder.OwnsMany(x => x.Lines, line =>
        {
            line.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderLines", VPureLuxConsts.DbSchema);
            line.WithOwner().HasForeignKey("SalesOrderId");
            line.HasKey(x => x.Id);
            line.Property(x => x.LineType).HasConversion<byte>().IsRequired();
            line.Property(x => x.ItemCodeSnapshot).HasMaxLength(SalesConsts.MaxCodeLength).IsRequired();
            line.Property(x => x.ItemNameSnapshot).HasMaxLength(SalesConsts.MaxNameLength).IsRequired();
            line.Property(x => x.UnitSnapshot).HasMaxLength(SalesConsts.MaxUnitLength).IsRequired();
            line.Property(x => x.Quantity).HasPrecision(SalesConsts.QuantityPrecision, SalesConsts.QuantityScale).IsRequired();
            ConfigureMoney(line.Property(x => x.SuggestedPriceSnapshot));
            ConfigureMoney(line.Property(x => x.ActualSellingPrice));
            line.Property(x => x.OverrideReason).HasColumnType($"nvarchar({SalesConsts.MaxOverrideReasonLength})");
            ConfigureMoney(line.Property(x => x.RevenueAmount));
            ConfigureMoney(line.Property(x => x.CostPriceSnapshot));
            ConfigureMoney(line.Property(x => x.CostAmountSnapshot));
            ConfigureMoney(line.Property(x => x.ProfitAmount));
            line.Property(x => x.MarginPercent).HasPrecision(SalesConsts.MarginPrecision, SalesConsts.MarginScale).IsRequired();
            line.HasOne<VPureLux.Bom.BomVersion>().WithMany().HasForeignKey(x => x.BomVersionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Pricing.ProductSuggestedPriceVersion>().WithMany().HasForeignKey(x => x.SuggestedPriceVersionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Restrict);
            line.HasIndex("SalesOrderId", nameof(SalesOrderLine.LineNo)).IsUnique().HasDatabaseName("UX_SalesOrderLines_OrderId_LineNo");
            line.HasIndex(x => new { x.LineType, x.CatalogItemId }).HasDatabaseName("IX_SalesOrderLines_LineType_CatalogItemId");

            line.OwnsMany(x => x.BomSnapshotItems, item =>
            {
                item.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderBomSnapshotItems", VPureLuxConsts.DbSchema);
                item.WithOwner().HasForeignKey("SalesOrderLineId");
                item.HasKey(x => x.Id);
                item.Property(x => x.ComponentCode).HasMaxLength(SalesConsts.MaxCodeLength).IsRequired();
                item.Property(x => x.ComponentName).HasMaxLength(SalesConsts.MaxNameLength).IsRequired();
                item.Property(x => x.Unit).HasMaxLength(SalesConsts.MaxUnitLength).IsRequired();
                item.Property(x => x.QuantityPerProduct).HasPrecision(SalesConsts.QuantityPrecision, SalesConsts.QuantityScale).IsRequired();
                item.Property(x => x.TotalRequiredQuantity).HasPrecision(SalesConsts.QuantityPrecision, SalesConsts.QuantityScale).IsRequired();
                item.HasOne<VPureLux.Catalog.Component>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Restrict);
            });
        });
    }

    private static void ConfigureMoney(PropertyBuilder<decimal> property) =>
        property.HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale).IsRequired();

    private static void ConfigureMoney(PropertyBuilder<decimal?> property) =>
        property.HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
}
