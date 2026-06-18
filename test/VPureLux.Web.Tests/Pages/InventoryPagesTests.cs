using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using VPureLux.Web.Pages.Inventory;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class InventoryPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Inventory_Query_And_Posting_Pages_Should_Render()
    {
        foreach (var route in new[] { "/Inventory", "/Inventory/Warehouses", "/Inventory/Receipt", "/Inventory/Issue", "/Inventory/Adjustment", "/Inventory/Balances", "/Inventory/Lots", "/Inventory/Ledger" })
        {
            (await GetResponseAsStringAsync(route)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Inventory_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        var model = new Web.Pages.Inventory.IndexModel(authorization)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } }
        };
        await model.OnGetAsync();
        model.CanReceive.ShouldBeFalse();
        model.CanIssue.ShouldBeFalse();
        model.CanAdjust.ShouldBeFalse();
        model.CanManageWarehouses.ShouldBeFalse();
        model.CanViewLedger.ShouldBeFalse();
    }

    [Fact]
    public async Task Receipt_Page_Should_Render_Warehouse_And_StockItem_Selectors()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-R"),
            Name = "Receipt Warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-R"),
            Name = "Receipt Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));

        html.ShouldContain($"{warehouse.Code} - {warehouse.Name}");
        html.ShouldContain($"{component.Code} - {component.Name}");
        html.ShouldContain("name=\"Input.IdempotencyKey\"");
        html.ShouldContain("type=\"hidden\"");
        html.ShouldNotContain("type=\"text\" id=\"Input_IdempotencyKey\"");
    }

    [Fact]
    public async Task Posting_Pages_Should_Render_Multi_Line_Ui_Hidden_Idempotency_And_Vietnamese_Dates()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-P"),
            Name = "Posting Warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-P"),
            Name = "Posting Component",
            Unit = "pcs"
        });

        var receiptHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));
        var issueHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Issue"));
        var adjustmentHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Adjustment"));

        foreach (var html in new[] { receiptHtml, issueHtml, adjustmentHtml })
        {
            html.ShouldContain($"{warehouse.Code} - {warehouse.Name}");
            html.ShouldContain($"{component.Code} - {component.Name}");
            html.ShouldContain("type=\"hidden\"");
            html.ShouldContain("data-inventory-posting-form");
            html.ShouldContain("data-inventory-line-container");
            html.ShouldContain("data-remove-line");
            html.ShouldContain("Thêm dòng");
            html.ShouldNotContain("type=\"text\" id=\"Input_IdempotencyKey\"");
            html.ShouldNotContain("type=\"text\" id=\"IdempotencyKey\"");
        }

        receiptHtml.ShouldContain("name=\"ReceivedAtTexts[0]\"");
        receiptHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        adjustmentHtml.ShouldContain("name=\"IncreaseReceivedAtTexts[0]\"");
        adjustmentHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
    }

    [Fact]
    public async Task Posting_Pages_Should_Register_External_Abp_Script_And_Avoid_Inline_Scripts()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Inventory/Receipt.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Issue.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldContain("@section scripts");
            pageSource.ShouldContain("<abp-script src=\"/Pages/Inventory/Posting.js\" />");
            pageSource.ShouldNotContain("<script>");
            pageSource.ShouldNotContain("<script src=");
            pageSource.ShouldNotContain("selected=\"@(option.Value");
        }

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.confirmed");
    }

    [Fact]
    public void InventoryPostingUi_Should_Parse_And_Format_Vietnamese_Dates()
    {
        InventoryPostingUi.FormatDate(new DateTime(2026, 6, 18)).ShouldBe("18/06/2026");
        InventoryPostingUi.TryParseDate("18/06/2026", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(new DateTime(2026, 6, 18));
        InventoryPostingUi.TryParseDate("18-06-2026", out _).ShouldBeFalse();
        InventoryPostingUi.TryParseDate(string.Empty, out _).ShouldBeFalse();
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

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
