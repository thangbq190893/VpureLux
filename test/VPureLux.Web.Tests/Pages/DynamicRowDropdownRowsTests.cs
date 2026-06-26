using System;
using System.IO;
using Shouldly;
using Xunit;

namespace VPureLux.Pages;

public class DynamicRowDropdownRowsTests
{
    [Fact]
    public void DynamicRowSelects_Should_Strip_Select2_And_Reinitialize_On_Clone()
    {
        var scriptSource = ReadRepoFile("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js");

        scriptSource.ShouldContain("stripSelect2Enhancements");
        scriptSource.ShouldContain(".select2-container");
        scriptSource.ShouldContain("select2('destroy')");
        scriptSource.ShouldContain("initializeSelects");
        scriptSource.ShouldContain("createCleanClone");
        scriptSource.ShouldContain("ensureTemplate");
        scriptSource.ShouldContain("data-dynamic-row-template");
        scriptSource.ShouldContain("window.vplDynamicRowSelects");
    }

    [Fact]
    public void BomItems_Should_Use_Hidden_Template_And_Initialize_New_Row_Selects()
    {
        var pageSource = ReadRepoFile("src/VPureLux.Web/Pages/Bom/Create.cshtml");
        var scriptSource = ReadRepoFile("src/VPureLux.Web/Pages/Bom/BomItems.js");

        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("class=\"form-select component-id\"");
        pageSource.ShouldNotContain("id=\"Items_0__ComponentId\"");

        scriptSource.ShouldContain("window.vplDynamicRowSelects");
        scriptSource.ShouldContain("ensureTemplate(container, rowSelector)");
        scriptSource.ShouldContain("createCleanClone(template)");
        scriptSource.ShouldContain("initializeSelects(row)");
        scriptSource.ShouldContain(".component-id");
        scriptSource.ShouldContain(":not([");
        scriptSource.ShouldNotContain("getElementById('Items_0");
    }

    [Fact]
    public void Inventory_Posting_Should_Use_Hidden_Template_And_Initialize_New_Row_Selects()
    {
        var pageSource = ReadRepoFile("src/VPureLux.Web/Pages/Inventory/Receipt.cshtml");
        var scriptSource = ReadRepoFile("src/VPureLux.Web/Pages/Inventory/Posting.js");

        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("data-inventory-line-row");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].StockItemId\"");
        pageSource.ShouldContain("data-id=\"Input_Lines___index____StockItemId\"");

        scriptSource.ShouldContain("window.vplDynamicRowSelects");
        scriptSource.ShouldContain("ensureTemplate(container, rowSelector)");
        scriptSource.ShouldContain("createCleanClone(template)");
        scriptSource.ShouldContain("initializeSelects(row)");
        scriptSource.ShouldContain("rowSelector + ':not([' + templateAttribute + '])'");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
