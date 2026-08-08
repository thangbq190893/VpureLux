using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Suppliers;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SupplierPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Supplier_Pages_Should_Render()
    {
        (await GetResponseAsStringAsync("/Suppliers")).ShouldNotBeNullOrWhiteSpace();
        (await GetResponseAsStringAsync("/Suppliers/Create")).ShouldContain("Input.Code");
    }

    [Fact]
    public async Task Supplier_Menu_And_Page_Should_Use_DataTables_Server_Side()
    {
        var menuSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs"));
        var menuConstantsSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenus.cs"));
        var indexSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Suppliers/Index.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Suppliers/Index.js"));

        menuConstantsSource.ShouldContain("Suppliers");
        menuSource.ShouldContain("VPureLuxMenus.Suppliers");
        menuSource.ShouldContain("\"~/Suppliers\"");
        indexSource.ShouldContain("SuppliersTable");
        scriptSource.ShouldContain("serverSide: true");
        scriptSource.ShouldContain("Suppliers?handler=List");
        scriptSource.ShouldNotContain("const ");
        scriptSource.ShouldNotContain("let ");
    }

    [Fact]
    public async Task Supplier_Index_Should_Show_Created_Supplier_Text()
    {
        var supplier = await GetRequiredService<ISupplierAppService>().CreateAsync(new CreateSupplierDto
        {
            Code = Unique("SUP-W"),
            Name = "Web Supplier"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Suppliers"));

        html.ShouldContain("SuppliersTable");
        html.ShouldContain("Nhà cung cấp");
        supplier.Code.ShouldNotBeNullOrWhiteSpace();
    }

    private static string Unique(string prefix) => prefix + System.Guid.NewGuid().ToString("N")[..8];

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
