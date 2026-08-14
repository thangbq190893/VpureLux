using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Localization;
using VPureLux.OperatingCosts;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class OperatingCostPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Operating_Cost_Pages_Should_Render_DataTables_Actions_And_Localized_Text()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var index = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/OperatingCosts"));
        var categories = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/OperatingCosts/Categories"));
        var indexScript = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/OperatingCosts/Index.js"));
        var indexStyles = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/OperatingCosts/Index.css"));
        var categoriesScript = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/OperatingCosts/Categories.js"));
        var ledgerScript = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.js"));
        var globalScript = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/wwwroot/global-scripts.js"));
        var menuSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs"));
        var webModuleSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/VPureLuxWebModule.cs"));

        index.ShouldContain(localizer["OperatingCosts:Title"].Value);
        index.ShouldContain("id=\"OperatingCostsTable\"");
        index.ShouldContain("operating-costs-summary");
        index.ShouldContain("operating-costs-filter-grid");
        index.ShouldContain("operating-costs-table");
        index.ShouldContain("/OperatingCosts/Create");
        index.ShouldContain(localizer["OperatingCosts:TotalIncome"].Value);
        index.ShouldContain(localizer["OperatingCosts:MonthDebt"].Value);
        categories.ShouldContain(localizer["OperatingCosts:Categories"].Value);
        categories.ShouldContain("id=\"OperatingCostCategoriesTable\"");
        categories.ShouldContain("/OperatingCosts/CreateCategory");

        indexScript.ShouldContain("serverSide: true");
        indexScript.ShouldContain("rowAction");
        indexScript.ShouldContain("OperatingCosts?handler=Summary");
        indexScript.ShouldContain("OperatingCosts?handler=Delete");
        indexScript.ShouldContain("operating-costs-description-cell");
        indexScript.ShouldContain("operating-costs-counterparty-cell");
        indexScript.ShouldContain("window.vPureLuxDate.toIso($fromDate.val())");
        indexScript.ShouldContain("window.vPureLuxDate.toIso($toDate.val())");
        indexStyles.ShouldContain("grid-template-columns");
        indexStyles.ShouldContain("grid-template-rows: 1.1rem 2.25rem");
        indexStyles.ShouldContain(".operating-costs-summary");
        indexStyles.ShouldContain("table-layout: fixed");
        indexStyles.ShouldContain("width: 8rem");
        indexStyles.ShouldContain("white-space: normal !important");
        indexStyles.ShouldContain("height: 2.25rem");
        indexStyles.ShouldContain("height: auto !important");
        categoriesScript.ShouldContain("serverSide: true");
        categoriesScript.ShouldContain("OperatingCosts/Categories?handler=Delete");
        ledgerScript.ShouldContain("window.vPureLuxDate.toIso($fromDate.val())");
        ledgerScript.ShouldContain("window.vPureLuxDate.toIso($toDate.val())");
        globalScript.ShouldContain("format: 'dd/mm/yyyy'");
        globalScript.ShouldContain("language: 'vi'");
        globalScript.ShouldContain("input.type = 'text'");
        globalScript.ShouldContain("window.vPureLuxDate");
        webModuleSource.ShouldContain("/libs/bootstrap-datepicker/locales/bootstrap-datepicker.vi.min.js");
        webModuleSource.IndexOf(
            "/libs/bootstrap-datepicker/locales/bootstrap-datepicker.vi.min.js",
            StringComparison.Ordinal).ShouldBeLessThan(
            webModuleSource.IndexOf("/global-scripts.js", StringComparison.Ordinal));
        menuSource.ShouldContain("VPureLuxMenus.OperatingCosts");
        menuSource.ShouldContain("VPureLuxPermissions.OperatingCosts.View");
        index.ShouldNotContain("Linh kiện", Case.Insensitive);
    }

    [Fact]
    public async Task Operating_Cost_Create_And_Edit_Pages_Should_Render_Active_Categories()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var service = GetRequiredService<IOperatingCostAppService>();
        var category = await service.CreateCategoryAsync(new CreateOperatingCostCategoryDto
        {
            Code = Unique("WEB-OPC"),
            Name = "Chi phí web",
            Direction = OperatingCostDirection.Expense
        });
        var entry = await service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = new DateTime(2026, 8, 13),
            Direction = OperatingCostDirection.Expense,
            CategoryId = category.Id,
            Amount = 250_000,
            PaymentStatus = OperatingCostPaymentStatus.Paid,
            Description = "Tiếp khách web"
        });

        var create = WebUtility.HtmlDecode(await GetOkBodyAsync("/OperatingCosts/Create"));
        var edit = WebUtility.HtmlDecode(await GetOkBodyAsync($"/OperatingCosts/Edit?id={entry.Id}"));
        var createCategory = WebUtility.HtmlDecode(await GetOkBodyAsync("/OperatingCosts/CreateCategory"));
        var editCategory = WebUtility.HtmlDecode(await GetOkBodyAsync($"/OperatingCosts/EditCategory?id={category.Id}"));

        create.ShouldContain(category.Name);
        edit.ShouldContain("Tiếp khách web");
        edit.ShouldContain("250000");
        createCategory.ShouldContain(localizer["OperatingCosts:CategoryCode"].Value);
        editCategory.ShouldContain(category.Code);
        editCategory.ShouldContain(category.Name);
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private async Task<string> GetOkBodyAsync(string url)
    {
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return body;
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
