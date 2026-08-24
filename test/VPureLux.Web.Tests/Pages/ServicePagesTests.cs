using System;
using System.IO;
using Shouldly;
using Xunit;

namespace VPureLux.Pages;

public class ServicePagesTests
{
    [Fact]
    public void Service_should_have_one_primary_menu_and_server_paged_lists()
    {
        var menu = Read("src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs");
        var permissionSeed = Read("src/VPureLux.Application/Permissions/VPureLuxPermissionDataSeedContributor.cs");
        var orders = Read("src/VPureLux.Web/Pages/Service/Index.js");
        var works = Read("src/VPureLux.Web/Pages/Service/Works.js");
        menu.ShouldContain("VPureLuxMenus.Service");
        menu.ShouldNotContain("ServiceWorks");
        permissionSeed.ShouldContain("VPureLuxPermissions.Service.View");
        permissionSeed.ShouldContain("VPureLuxPermissions.Reports.Service.View");
        orders.ShouldContain("serverSide: true");
        works.ShouldContain("serverSide: true");
    }

    [Fact]
    public void Service_create_should_reuse_customer_asset_and_remote_lookups()
    {
        var page = Read("src/VPureLux.Web/Pages/Service/Create.cshtml");
        var model = Read("src/VPureLux.Web/Pages/Service/Create.cshtml.cs");
        var script = Read("src/VPureLux.Web/Pages/Service/EditForm.js");
        page.ShouldContain("Input.CustomerAssetId");
        page.ShouldContain("data-add-service-line=\"Material\"");
        page.ShouldContain("data-add-service-line=\"Labor\"");
        model.ShouldContain("GetAssetOptionsAsync");
        model.ShouldNotContain("LimitedResultRequestDto.MaxMaxResultCount");
        script.ShouldContain("minimumInputLength");
        page.ShouldContain("js-select2");
        script.ShouldContain("stripLeptonXSelectEnhancements");
        script.ShouldNotContain("window.prompt");
        script.ShouldNotContain("window.alert");
        script.ShouldNotContain("window.confirm");
    }

    [Fact]
    public void Completion_and_payment_should_use_abp_modals()
    {
        var details = Read("src/VPureLux.Web/Pages/Service/Details.js");
        var completion = Read("src/VPureLux.Web/Pages/Service/CompleteModal.cshtml.cs");
        details.ShouldContain("new abp.ModalManager");
        details.ShouldContain("CompleteServiceButton");
        details.ShouldContain("AddServicePaymentButton");
        details.ShouldContain("data-void-payment");
        completion.ShouldContain("IdempotencyKey");
        completion.ShouldContain("CompleteAsync");
    }

    [Fact]
    public void Reports_should_use_server_paging_and_keep_sales_report_separate()
    {
        var script = Read("src/VPureLux.Web/Pages/Reports/BusinessRevenue.js");
        var repository = Read("src/VPureLux.EntityFrameworkCore/Reports/EfCoreBusinessRevenueReadRepository.cs");
        var salesReport = Read("src/VPureLux.EntityFrameworkCore/Reports/EfCoreSalesReportReadRepository.cs");
        script.ShouldContain("serverSide: true");
        repository.ShouldContain("sales.Concat(service)");
        repository.ShouldContain("ServiceOrderStatus.Completed");
        salesReport.ShouldNotContain("ServiceOrderStatus");
    }

    private static string Read(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new FileNotFoundException(relativePath);
    }
}
