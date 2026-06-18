using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace VPureLux.Audit;

public class BusinessAuditLogConfiguration : IEntityTypeConfiguration<BusinessAuditLog>
{
    public void Configure(EntityTypeBuilder<BusinessAuditLog> builder)
    {
        builder.ToTable(VPureLuxConsts.DbTablePrefix + "BusinessAuditLogs", VPureLuxConsts.DbSchema);
        builder.ConfigureByConvention();
        builder.Property(x => x.Module).HasMaxLength(AuditConsts.MaxModuleLength).IsRequired();
        builder.Property(x => x.EventName).HasMaxLength(AuditConsts.MaxEventNameLength).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(AuditConsts.MaxActionLength).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(AuditConsts.MaxEntityTypeLength).IsRequired();
        builder.Property(x => x.EntityDisplay).HasMaxLength(AuditConsts.MaxEntityDisplayLength);
        builder.Property(x => x.ActorType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.UserNameSnapshot).HasMaxLength(AuditConsts.MaxUserNameLength);
        builder.Property(x => x.CorrelationId).HasMaxLength(AuditConsts.MaxCorrelationIdLength).IsRequired();
        builder.Property(x => x.CausationId).HasMaxLength(AuditConsts.MaxCorrelationIdLength).IsRequired();
        builder.Property(x => x.Severity).HasConversion<byte>().IsRequired();
        builder.Property(x => x.OldValueJson);
        builder.Property(x => x.NewValueJson);
        builder.Property(x => x.MetadataJson);
        builder.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UX_BusinessAuditLogs_EventId");
        builder.HasIndex(x => x.EventTime).HasDatabaseName("IX_BusinessAuditLogs_EventTime");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_BusinessAuditLogs_UserId");
        builder.HasIndex(x => x.Module).HasDatabaseName("IX_BusinessAuditLogs_Module");
        builder.HasIndex(x => x.EntityType).HasDatabaseName("IX_BusinessAuditLogs_EntityType");
        builder.HasIndex(x => x.EntityId).HasDatabaseName("IX_BusinessAuditLogs_EntityId");
        builder.HasIndex(x => x.Severity).HasDatabaseName("IX_BusinessAuditLogs_Severity");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_BusinessAuditLogs_CorrelationId");
    }
}
