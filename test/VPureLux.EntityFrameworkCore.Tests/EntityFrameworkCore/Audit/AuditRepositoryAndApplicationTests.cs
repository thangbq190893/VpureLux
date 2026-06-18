using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using global::VPureLux.Audit;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Audit;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class AuditRepositoryAndApplicationTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_Define_Permissions_And_Protect_Application_Service()
    {
        var definitions = GetRequiredService<IPermissionDefinitionManager>();
        (await definitions.GetAsync(VPureLuxPermissions.Audit.View)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Audit.Export)).ShouldNotBeNull();
        typeof(BusinessAuditAppService).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(VPureLuxPermissions.Audit.View);
        typeof(BusinessAuditAppService).GetMethod(nameof(BusinessAuditAppService.ExportAsync))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.Export);
    }

    [Fact]
    public async Task Should_Persist_Search_Detail_Report_And_Export_With_Export_Audits()
    {
        var log = await InsertAsync("Pricing", "PRICE_CREATED");
        var service = GetRequiredService<IBusinessAuditAppService>();

        (await service.GetAsync(log.Id)).EventId.ShouldBe(log.EventId);
        (await service.GetListAsync(new AuditSearchInput { Module = "Pricing" })).Items
            .ShouldContain(x => x.Id == log.Id);
        (await service.GetReportAsync("price-changes", new AuditSearchInput())).Items
            .ShouldContain(x => x.Id == log.Id);
        Encoding.UTF8.GetString((await service.ExportAsync(new AuditSearchInput { Module = "Pricing" })).Content)
            .ShouldContain("PRICE_CREATED");

        var auditRows = await service.GetListAsync(new AuditSearchInput { Module = "Audit", MaxResultCount = 100 });
        auditRows.Items.ShouldContain(x => x.Action == AuditActionTypes.ExportRequested);
        auditRows.Items.ShouldContain(x => x.Action == AuditActionTypes.ExportCompleted);
    }

    [Fact]
    public async Task Should_Map_Required_Table_Indexes_And_No_Soft_Delete()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var entity = db.Model.FindEntityType(typeof(BusinessAuditLog))!;
            entity.GetTableName().ShouldBe("AppBusinessAuditLogs");
            entity.FindProperty(nameof(BusinessAuditLog.ActorType))!.ClrType.ShouldBe(typeof(AuditActorType));
            entity.FindProperty("IsDeleted").ShouldBeNull();
            foreach (var index in new[]
                     {
                         "UX_BusinessAuditLogs_EventId", "IX_BusinessAuditLogs_EventTime",
                         "IX_BusinessAuditLogs_UserId", "IX_BusinessAuditLogs_Module",
                         "IX_BusinessAuditLogs_EntityType", "IX_BusinessAuditLogs_EntityId",
                         "IX_BusinessAuditLogs_Severity", "IX_BusinessAuditLogs_CorrelationId"
                     })
            {
                entity.GetIndexes().ShouldContain(x => x.GetDatabaseName() == index);
            }
            entity.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BusinessAuditLogs_EventId").IsUnique.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Event_Id_Should_Be_Database_Unique()
    {
        var eventId = Guid.NewGuid();
        await InsertAsync("Audit", "FIRST", eventId);
        await Should.ThrowAsync<DbUpdateException>(() => InsertAsync("Audit", "SECOND", eventId));
    }

    [Fact]
    public async Task Update_And_Delete_Should_Fail_With_Database_Trigger()
    {
        var log = await InsertAsync("Audit", "IMMUTABLE");
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER IF NOT EXISTS TR_AppBusinessAuditLogs_Immutable_Update
                BEFORE UPDATE ON AppBusinessAuditLogs BEGIN SELECT RAISE(FAIL, 'immutable'); END;
                CREATE TRIGGER IF NOT EXISTS TR_AppBusinessAuditLogs_Immutable_Delete
                BEFORE DELETE ON AppBusinessAuditLogs BEGIN SELECT RAISE(FAIL, 'immutable'); END;
                """);
        });

        await Should.ThrowAsync<Exception>(() => WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE AppBusinessAuditLogs SET Module = 'Changed' WHERE Id = {0}", log.Id);
        }));
        await Should.ThrowAsync<Exception>(() => WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM AppBusinessAuditLogs WHERE Id = {0}", log.Id);
        }));
    }

    [Fact]
    public void Migration_Should_Contain_SQL_Server_Append_Only_Trigger()
    {
        var root = FindRepositoryRoot();
        var migration = System.IO.Directory.GetFiles(
                System.IO.Path.Combine(root, "src", "VPureLux.EntityFrameworkCore", "Migrations"),
                "*_AddAuditModule.cs")
            .Single();
        var text = System.IO.File.ReadAllText(migration);
        text.ShouldContain("TR_AppBusinessAuditLogs_Immutable");
        text.ShouldContain("INSTEAD OF UPDATE, DELETE");
        text.ShouldContain("THROW 51000");
    }

    private async Task<BusinessAuditLog> InsertAsync(string module, string action, Guid? eventId = null)
    {
        var manager = GetRequiredService<BusinessAuditManager>();
        var repository = GetRequiredService<IBusinessAuditLogRepository>();
        var log = manager.Create(new BusinessAuditEnvelope(
            eventId ?? Guid.NewGuid(), module, action, action, "Entity", Guid.NewGuid(),
            Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), DateTime.UtcNow));
        await WithUnitOfWorkAsync(() => repository.InsertAsync(log, autoSave: true));
        return log;
    }

    private static string FindRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        while (path != null && !System.IO.File.Exists(System.IO.Path.Combine(path, "VPureLux.slnx")))
        {
            path = System.IO.Directory.GetParent(path)?.FullName;
        }
        return path ?? throw new System.IO.DirectoryNotFoundException();
    }
}
