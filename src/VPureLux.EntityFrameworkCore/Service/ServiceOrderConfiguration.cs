using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Service;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public const string OrderNoUniqueIndexName = "UX_ServiceOrders_OrderNo";
    public const string CompletionKeyUniqueIndexName = "UX_ServiceOrders_CompletionIdempotencyKey";

    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "ServiceOrders", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.OrderNo).HasMaxLength(ServiceConsts.MaxOrderNoLength).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.CustomerCodeSnapshot).HasMaxLength(ServiceConsts.MaxCodeLength).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(ServiceConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.AssetNoSnapshot).HasMaxLength(ServiceConsts.MaxAssetNoLength).IsRequired();
        builder.Property(x => x.AssetNameSnapshot).HasMaxLength(ServiceConsts.MaxNameLength).IsRequired();
        builder.Property(x => x.ServiceAddress).HasMaxLength(ServiceConsts.MaxAddressLength);
        builder.Property(x => x.Note).HasMaxLength(ServiceConsts.MaxNoteLength);
        builder.Property(x => x.CancellationReason).HasMaxLength(ServiceConsts.MaxNoteLength);
        builder.Property(x => x.CompletionIdempotencyKey).HasMaxLength(ServiceConsts.MaxIdempotencyKeyLength);
        builder.Property(x => x.CompletionCommandHash).HasMaxLength(64);
        builder.Property(x => x.ActualCostAmount).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale);
        builder.Property(x => x.ActualProfitAmount).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale);
        ConfigureMoney(builder.Property(x => x.TotalRevenueAmount));
        ConfigureMoney(builder.Property(x => x.TotalCostAmount));
        ConfigureMoney(builder.Property(x => x.TotalProfitAmount));
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<VPureLux.Customers.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Warranty.CustomerAsset>().WithMany().HasForeignKey(x => x.CustomerAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Inventory.Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OrderNo).IsUnique().HasDatabaseName(OrderNoUniqueIndexName);
        builder.HasIndex(x => x.CompletionIdempotencyKey).IsUnique()
            .HasFilter("[CompletionIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName(CompletionKeyUniqueIndexName);
        builder.HasIndex(x => new { x.Status, x.OrderDate }).HasDatabaseName("IX_ServiceOrders_Status_OrderDate");
        builder.HasIndex(x => new { x.CustomerAssetId, x.OrderDate }).HasDatabaseName("IX_ServiceOrders_AssetId_OrderDate");

        builder.OwnsMany(x => x.Lines, line =>
        {
            line.ToTable(VPureLuxConsts.DbTablePrefix + "ServiceOrderLines", VPureLuxConsts.DbSchema);
            line.WithOwner().HasForeignKey("ServiceOrderId");
            line.HasKey(x => x.Id);
            line.Property(x => x.LineType).HasConversion<byte>().IsRequired();
            line.Property(x => x.ItemCodeSnapshot).HasMaxLength(ServiceConsts.MaxCodeLength).IsRequired();
            line.Property(x => x.ItemNameSnapshot).HasMaxLength(ServiceConsts.MaxNameLength).IsRequired();
            line.Property(x => x.UnitSnapshot).HasMaxLength(ServiceConsts.MaxUnitLength).IsRequired();
            ConfigureMoney(line.Property(x => x.UnitPrice));
            ConfigureMoney(line.Property(x => x.RevenueAmount));
            ConfigureMoney(line.Property(x => x.CostAmountSnapshot));
            line.Property(x => x.StandardCostSnapshot).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale);
            line.Property(x => x.ActualCostAmount).HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale);
            line.Property(x => x.Note).HasMaxLength(ServiceConsts.MaxNoteLength);
            line.HasOne<VPureLux.Catalog.Component>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Warranty.CustomerAssetComponent>().WithMany().HasForeignKey(x => x.CustomerAssetComponentId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<ServiceWork>().WithMany().HasForeignKey(x => x.ServiceWorkId).OnDelete(DeleteBehavior.Restrict);
            line.HasIndex("ServiceOrderId", nameof(ServiceOrderLine.LineNo)).IsUnique().HasDatabaseName("UX_ServiceOrderLines_OrderId_LineNo");
            line.HasIndex(x => new { x.LineType, x.ComponentId }).HasDatabaseName("IX_ServiceOrderLines_Type_ComponentId");
        });
    }

    private static void ConfigureMoney(PropertyBuilder<decimal> property) =>
        property.HasPrecision(ServiceConsts.MoneyPrecision, ServiceConsts.MoneyScale).IsRequired();
}
