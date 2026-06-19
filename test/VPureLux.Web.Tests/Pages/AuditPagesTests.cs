using System;
using System.IO;
using System.Net;
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
using VPureLux.Web.Pages.Audit;
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
    public async Task Audit_Index_Should_Render_Localized_Badges_And_Entity_Display()
    {
        var log = await InsertAsync(
            module: "Audit",
            eventName: "AUDIT_EXPORT_COMPLETED",
            action: AuditActionTypes.ExportCompleted,
            entityType: "AuditExport",
            entityDisplay: "Xuất nhật ký 5 dòng",
            severity: AuditSeverity.Critical,
            actorType: AuditActorType.User,
            userName: "support",
            isSystemGenerated: false);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Audit"));

        html.ShouldContain("Xuất nhật ký 5 dòng");
        html.ShouldContain("Hoàn tất xuất nhật ký");
        html.ShouldContain("Nghiêm trọng");
        html.ShouldContain("Ghi nhận");
        html.ShouldContain("Người dùng");
        html.ShouldContain("text-bg-danger");
        html.ShouldContain($"/Audit/Details/{log.Id}");
    }

    [Fact]
    public async Task Audit_Detail_Should_Render_Readable_Sections_And_Encoded_Formatted_Json()
    {
        var log = await InsertAsync(
            module: "Customer",
            eventName: "CUSTOMER_UPDATED",
            action: AuditActionTypes.Update,
            entityType: "Customer",
            entityDisplay: "CUST-001 - Khách thử nghiệm",
            severity: AuditSeverity.Important,
            actorType: AuditActorType.User,
            userName: "operator",
            oldValueJson: "{\"Name\":\"<b>Old</b>\"}",
            newValueJson: "{\"Name\":\"New\",\"Status\":\"Active\"}",
            metadataJson: "{\"Reason\":\"UAT\"}");

        var html = await GetResponseAsStringAsync($"/Audit/Details/{log.Id}");
        var decoded = WebUtility.HtmlDecode(html);

        decoded.ShouldContain("Tóm tắt");
        decoded.ShouldContain("Tác nhân và nguồn");
        decoded.ShouldContain("Ngữ cảnh đối tượng");
        decoded.ShouldContain("Dữ liệu sự kiện");
        decoded.ShouldContain("Thông tin hỗ trợ kỹ thuật");
        decoded.ShouldContain("CUST-001 - Khách thử nghiệm");
        decoded.ShouldContain("Mã kỹ thuật đối tượng (hỗ trợ)");
        decoded.ShouldContain("Quan trọng");
        decoded.ShouldContain("\"Status\": \"Active\"");
        html.ShouldContain("&lt;b&gt;Old&lt;/b&gt;");
        html.ShouldNotContain("<b>Old</b>");
    }

    [Fact]
    public void AuditUiFormatter_Should_Format_Valid_Json_And_Keep_Invalid_Payload_Text()
    {
        AuditUiFormatter.FormatJson("{\"Name\":\"Test\",\"Quantity\":2}")
            .ShouldContain("\"Quantity\": 2");
        AuditUiFormatter.FormatJson("not-json").ShouldBe("not-json");
        AuditUiFormatter.FormatJson(null).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Audit_Export_Page_Should_Render_Confirmation_And_Busy_Hooks()
    {
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Audit/Export"));

        html.ShouldContain("data-audit-export-form");
        html.ShouldContain("data-confirm-message");
        html.ShouldContain("data-started-message");
        html.ShouldContain("Bạn có chắc muốn xuất nhật ký nghiệp vụ");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Audit/Export.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Audit/Export.js\" />");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
        pageSource.ShouldNotContain("<abp-button href=");
        pageSource.ShouldNotContain("href=\"/");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Audit/Export.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("abp.notify.info");
        scriptSource.ShouldContain("dataset.confirmed");
    }

    [Fact]
    public async Task Audit_Razor_Pages_Should_Stay_Script_And_Link_Compliant()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Audit/Index.cshtml",
            "src/VPureLux.Web/Pages/Audit/Details.cshtml",
            "src/VPureLux.Web/Pages/Audit/Export.cshtml",
            "src/VPureLux.Web/Pages/Audit/Reports.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldNotContain("<abp-button href=");
            pageSource.ShouldNotContain("href=\"/");
            pageSource.ShouldNotContain("<script>");
            pageSource.ShouldNotContain("<script src=");
        }
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

    private async Task<BusinessAuditLog> InsertAsync(
        string module = "Page",
        string eventName = "PAGE_TEST",
        string action = "READ",
        string entityType = "Entity",
        string? entityDisplay = null,
        AuditSeverity severity = AuditSeverity.Informational,
        AuditActorType actorType = AuditActorType.System,
        string? userName = null,
        bool isSystemGenerated = false,
        string? oldValueJson = null,
        string? newValueJson = null,
        string? metadataJson = null)
    {
        var manager = GetRequiredService<BusinessAuditManager>();
        var repository = GetRequiredService<IBusinessAuditLogRepository>();
        var log = manager.Create(new BusinessAuditEnvelope(
            Guid.NewGuid(),
            module,
            eventName,
            action,
            entityType,
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"),
            DateTime.UtcNow,
            severity,
            entityDisplay,
            oldValueJson,
            newValueJson,
            metadataJson,
            UserId: actorType == AuditActorType.User ? Guid.NewGuid() : null,
            UserName: userName,
            ActorType: actorType,
            IsSystemGenerated: isSystemGenerated));
        var uowManager = GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin();
        await repository.InsertAsync(log, autoSave: true);
        await uow.CompleteAsync();
        return log;
    }

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
