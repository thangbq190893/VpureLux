using System;
using System.IO;
using System.Text.RegularExpressions;
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
        scriptSource.ShouldContain("querySelectorAll('[data-select2-id]')");
        scriptSource.ShouldContain("removeAttribute('data-select2-id')");
        scriptSource.ShouldContain("select2TargetSelector");
        scriptSource.ShouldContain("data-use-select2=\"true\"");
        scriptSource.ShouldContain("stripLeptonXSelectEnhancements");
        scriptSource.ShouldContain(".custom-select-wrapper[data-lpx-bound]");
        scriptSource.ShouldContain(".custom-select-display, .custom-options-container");
        scriptSource.ShouldContain("data-lpx-sync-bound");
        scriptSource.ShouldContain("form-select-lg");
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

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("class=\"vpl-select2-target component-id js-select2\"");
        pageSource.ShouldContain("data-use-select2=\"true\"");
        pageSource.ShouldNotContain("class=\"form-select form-select-sm component-id\"");
        pageSource.ShouldContain("data-line-editor-row");
        pageSource.ShouldContain("data-name=\"Items[__index__].ComponentId\"");
        pageSource.ShouldContain("data-id=\"Items___index____ComponentId\"");
        pageSource.ShouldNotContain("data-dynamic-select2=\"disabled\"");

        scriptSource.ShouldContain("window.vplDynamicRowSelects");
        scriptSource.ShouldContain("ensureTemplate(container, rowSelector)");
        scriptSource.ShouldContain("getLiveRows(container).forEach(function (row)");
        scriptSource.ShouldContain("stripSelect2Enhancements(row)");
        scriptSource.ShouldContain("stripLeptonXSelectEnhancements(row)");
        scriptSource.ShouldContain("createCleanClone(template)");
        scriptSource.ShouldContain("initializeSelects(row, '.component-id')");
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
        scriptSource.ShouldContain("stripLeptonXSelectEnhancements(row)");
        scriptSource.ShouldContain("initializeSelects(row, '.stock-item-id')");
        scriptSource.ShouldContain("rowSelector + ':not([' + templateAttribute + '])'");
    }

    [Fact]
    public void Select2_Targets_Should_Not_Render_With_LeptonX_Form_Select_Classes()
    {
        var select2PagePaths = new[]
        {
            "src/VPureLux.Web/Pages/Sales/Create.cshtml",
            "src/VPureLux.Web/Pages/Sales/Edit.cshtml",
            "src/VPureLux.Web/Pages/Bom/Create.cshtml",
            "src/VPureLux.Web/Pages/Bom/Edit.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Receipt.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Issue.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"
        };

        foreach (var path in select2PagePaths)
        {
            var pageSource = ReadRepoFile(path);
            var selectTags = Regex.Matches(
                pageSource,
                "<select(?=[^>]*(?:data-use-select2=\"true\"|js-select2))[^>]*>",
                RegexOptions.Singleline);

            selectTags.Count.ShouldBeGreaterThan(0, path);

            foreach (Match match in selectTags)
            {
                var selectTag = match.Value;
                selectTag.ShouldContain("data-use-select2=\"true\"");
                selectTag.ShouldContain("js-select2");
                selectTag.ShouldNotContain("form-select");
                selectTag.ShouldNotContain("form-select-sm");
                selectTag.ShouldNotContain("form-select-lg");
            }
        }
    }

    [Fact]
    public void LineEditor_Css_Should_Allow_Vertical_Dropdowns_And_Keep_Only_Horizontal_Scroll()
    {
        var cssSource = ReadRepoFile("src/VPureLux.Web/Pages/Shared/LineEditors.css");
        var dynamicSelectSource = ReadRepoFile("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js");
        var normalizedCssSource = cssSource.Replace("\r\n", "\n");

        cssSource.ShouldContain(".vpl-line-editor");
        cssSource.ShouldContain("overflow: visible !important");
        cssSource.ShouldContain(".vpl-line-editor.table-responsive");
        cssSource.ShouldNotContain("overflow-y: auto");
        cssSource.ShouldNotContain("overflow-y: scroll");
        cssSource.ShouldNotContain("overflow-y: hidden");
        cssSource.ShouldContain("min-width: 5.5rem");
        cssSource.ShouldContain("white-space: nowrap");
        normalizedCssSource.ShouldNotContain("\nheight:");
        normalizedCssSource.ShouldNotContain("\n    height:");
        normalizedCssSource.ShouldNotContain("max-height:");
        dynamicSelectSource.ShouldContain("closest('.modal, .offcanvas, #SalesCreatePage')");
        dynamicSelectSource.ShouldNotContain("#SalesCreatePage, form");
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
