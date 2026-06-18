using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using VPureLux.Audit;
using VPureLux.Permissions;
using Volo.Abp.Uow;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class AuditPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Audit_List_Reports_And_Export_Pages_Should_Render()
    {
        foreach (var route in new[] { "/Audit", "/Audit/Reports", "/Audit/Export" })
        {
            (await GetResponseAsStringAsync(route)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Audit_Detail_Page_Should_Render()
    {
        var manager = GetRequiredService<BusinessAuditManager>();
        var repository = GetRequiredService<IBusinessAuditLogRepository>();
        var log = manager.Create(new BusinessAuditEnvelope(
            Guid.NewGuid(), "Page", "PAGE_TEST", "READ", "Entity", Guid.NewGuid(),
            Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), DateTime.UtcNow));
        var uowManager = GetRequiredService<IUnitOfWorkManager>();
        using (var uow = uowManager.Begin())
        {
            await repository.InsertAsync(log, autoSave: true);
            await uow.CompleteAsync();
        }

        (await GetResponseAsStringAsync($"/Audit/Details/{log.Id}")).ShouldContain("Page");
    }

    [Fact]
    public async Task Export_Action_Should_Be_Permission_Aware()
    {
        var service = Substitute.For<IBusinessAuditAppService>();
        service.GetListAsync(Arg.Any<AuditSearchInput>())
            .Returns(new Volo.Abp.Application.Dtos.PagedResultDto<BusinessAuditLogDto>());
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), VPureLuxPermissions.Audit.Export)
            .Returns(AuthorizationResult.Failed());
        var model = new Web.Pages.Audit.IndexModel(service, authorization)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        await model.OnGetAsync();

        model.CanExport.ShouldBeFalse();
    }

    [Fact]
    public void Razor_Page_Models_Should_Use_Audit_Permissions()
    {
        typeof(Web.Pages.Audit.IndexModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.View);
        typeof(Web.Pages.Audit.DetailsModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.View);
        typeof(Web.Pages.Audit.ReportsModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.View);
        typeof(Web.Pages.Audit.ExportModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Audit.Export);
    }
}
