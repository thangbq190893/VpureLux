using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using VPureLux.Audit;
using VPureLux.Permissions;
using Volo.Abp.Uow;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class AuditApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Search_Detail_Reports_And_Export_Routes_Should_Work()
    {
        var log = await InsertAsync();

        (await GetResponseAsObjectAsync<Volo.Abp.Application.Dtos.PagedResultDto<BusinessAuditLogDto>>(
            "/api/audit/logs?module=API")).Items.ShouldContain(x => x.Id == log.Id);
        (await GetResponseAsObjectAsync<BusinessAuditLogDto>($"/api/audit/logs/{log.Id}")).Id.ShouldBe(log.Id);
        (await GetResponseAsObjectAsync<Volo.Abp.Application.Dtos.PagedResultDto<BusinessAuditLogDto>>(
            "/api/audit/reports/price-changes")).ShouldNotBeNull();

        var export = await Client.PostAsJsonAsync("/api/audit/exports", new AuditSearchInput { Module = "API" });
        export.StatusCode.ShouldBe(HttpStatusCode.OK);
        export.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
    }

    [Fact]
    public void Controller_Should_Delegate_To_Authorized_App_Service_And_Expose_No_Mutations()
    {
        typeof(BusinessAuditAppService).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(VPureLuxPermissions.Audit.View);
        typeof(BusinessAuditAppService).GetMethod(nameof(BusinessAuditAppService.ExportAsync))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.Export);
        var methods = typeof(BusinessAuditController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        methods.ShouldNotContain(x => x.GetCustomAttribute<HttpPutAttribute>() != null);
        methods.ShouldNotContain(x => x.GetCustomAttribute<HttpDeleteAttribute>() != null);
    }

    private async Task<BusinessAuditLog> InsertAsync()
    {
        var manager = GetRequiredService<BusinessAuditManager>();
        var repository = GetRequiredService<IBusinessAuditLogRepository>();
        var log = manager.Create(new BusinessAuditEnvelope(
            Guid.NewGuid(), "API", "API_TEST", "READ", "Entity", Guid.NewGuid(),
            Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), DateTime.UtcNow));
        var uowManager = GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin();
        await repository.InsertAsync(log, autoSave: true);
        await uow.CompleteAsync();
        return log;
    }
}
