using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VPureLux.Inventory;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Sales;

public class SalesOrderRevisionConfiguration : IEntityTypeConfiguration<SalesOrderRevision>
{
    public const string ActiveRevisionIndexName = "UX_SalesOrderRevisions_ActiveOrder";
    public const string ApplyKeyIndexName = "UX_SalesOrderRevisions_ApplyKey";

    public void Configure(EntityTypeBuilder<SalesOrderRevision> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderRevisions", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(SalesConsts.MaxReasonLength).IsRequired();
        builder.Property(x => x.ApplyIdempotencyKey).HasMaxLength(SalesConsts.MaxIdempotencyKeyLength);
        Money(builder.Property(x => x.BeforeTotal));
        Money(builder.Property(x => x.AppliedTotal));
        Money(builder.Property(x => x.RefundDue));
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SalesOrderId, x.RevisionNo }).IsUnique();
        builder.HasIndex(x => x.SalesOrderId).IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName(ActiveRevisionIndexName);
        builder.HasIndex(x => x.ApplyIdempotencyKey).IsUnique()
            .HasFilter("[ApplyIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName(ApplyKeyIndexName);

        builder.OwnsMany<SalesOrderRevisionLine>(nameof(SalesOrderRevision.Lines), line =>
        {
            line.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderRevisionLines", VPureLuxConsts.DbSchema);
            line.WithOwner().HasForeignKey("SalesOrderRevisionId");
            line.HasKey(x => x.Id);
            line.Property(x => x.Quantity).HasPrecision(SalesConsts.QuantityPrecision, SalesConsts.QuantityScale);
            line.Property(x => x.BeforeQuantity).HasPrecision(SalesConsts.QuantityPrecision, SalesConsts.QuantityScale);
            Money(line.Property(x => x.SuggestedPriceSnapshot));
            Money(line.Property(x => x.ActualSellingPrice));
            Money(line.Property(x => x.BeforeActualSellingPrice));
            Money(line.Property(x => x.BeforeCostAmount));
            Money(line.Property(x => x.AppliedCostAmount));
            line.Property(x => x.OverrideReason).HasMaxLength(SalesConsts.MaxOverrideReasonLength);
            line.Property(x => x.ReturnReason).HasMaxLength(SalesConsts.MaxReasonLength);
            line.HasOne<VPureLux.Bom.BomVersion>().WithMany().HasForeignKey(x => x.BomVersionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Bom.BomVersion>().WithMany().HasForeignKey(x => x.BeforeBomVersionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.BeforeInventoryTransactionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.IssueInventoryTransactionId).OnDelete(DeleteBehavior.Restrict);
            line.HasOne<VPureLux.Inventory.InventoryTransaction>().WithMany().HasForeignKey(x => x.ReversalInventoryTransactionId).OnDelete(DeleteBehavior.Restrict);
            line.HasIndex("SalesOrderRevisionId", nameof(SalesOrderRevisionLine.LineNo));

            line.OwnsMany<SalesOrderRevisionAllocation>(nameof(SalesOrderRevisionLine.ReversedAllocations), allocation =>
            {
                allocation.ToTable(VPureLuxConsts.DbTablePrefix + "SalesOrderRevisionAllocations", VPureLuxConsts.DbSchema);
                allocation.WithOwner().HasForeignKey("SalesOrderRevisionLineId");
                allocation.HasKey(x => x.Id);
                allocation.Property(x => x.Quantity).HasPrecision(InventoryConsts.QuantityPrecision, InventoryConsts.QuantityScale);
                allocation.Property(x => x.UnitCost).HasPrecision(InventoryConsts.CostPrecision, InventoryConsts.CostScale);
                allocation.HasOne<VPureLux.Inventory.StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
                allocation.HasOne<VPureLux.Inventory.InventoryLot>().WithMany().HasForeignKey(x => x.InventoryLotId).OnDelete(DeleteBehavior.Restrict);
            });
        });
    }

    private static void Money(PropertyBuilder<decimal> property) =>
        property.HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
    private static void Money(PropertyBuilder<decimal?> property) =>
        property.HasPrecision(SalesConsts.MoneyPrecision, SalesConsts.MoneyScale);
}
