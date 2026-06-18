using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Bom_Index_Should_Render_Product_Selector()
    {
        var product = await CreateProductAsync("BOM-IDX", "BOM Product Selector");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Bom"));

        html.ShouldContain($"{product.Code} - {product.Name}");
        html.ShouldContain("name=\"ProductId\"");
        html.ShouldContain("<select");
        html.ShouldNotContain("type=\"text\" id=\"ProductId\"");
    }

    [Fact]
    public async Task Bom_Create_Should_Render_Component_Selector_And_External_Script()
    {
        var product = await CreateProductAsync("BOM-CRT-P", "BOM Create Product");
        var component = await CreateComponentAsync("BOM-CRT-C", "BOM Create Component");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Create/{product.Id}"));

        html.ShouldContain($"{component.Code} - {component.Name}");
        html.ShouldContain("name=\"Items[0].ComponentId\"");
        html.ShouldContain("<select");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Create.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Bom/BomItems.js\" />");
        pageSource.ShouldNotContain("<script>");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/BomItems.js"));
        scriptSource.ShouldContain("component.innerHTML = sourceComponent.innerHTML");
    }

    [Fact]
    public async Task Bom_Product_And_Details_Should_Render_Catalog_Labels_Formatted_Dates_And_Integer_Quantity()
    {
        var product = await CreateProductAsync("BOM-LBL-P", "BOM Label Product");
        var component = await CreateComponentAsync("BOM-LBL-C", "BOM Label Component");
        var effectiveFrom = new DateTime(2026, 6, 18);
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = effectiveFrom,
            Items =
            [
                new CreateBomItemDto
                {
                    ComponentId = component.Id,
                    Quantity = 1
                }
            ]
        });

        var productHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        var detailsHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Details/{bom.Id}"));

        productHtml.ShouldContain($"{product.Code} - {product.Name}");
        detailsHtml.ShouldContain($"{product.Code} - {product.Name}");
        detailsHtml.ShouldContain($"{component.Code} - {component.Name}");
        productHtml.ShouldContain("18/06/2026");
        detailsHtml.ShouldContain("18/06/2026");
        detailsHtml.ShouldContain(">1</td>");
        detailsHtml.ShouldNotContain("1,0000");
        detailsHtml.ShouldNotContain("1.0000");
    }

    [Fact]
    public async Task Bom_Create_And_Clone_Should_Use_Vietnamese_Date_Text()
    {
        var product = await CreateProductAsync("BOM-DATE-P", "BOM Date Product");
        var component = await CreateComponentAsync("BOM-DATE-C", "BOM Date Component");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items =
            [
                new CreateBomItemDto
                {
                    ComponentId = component.Id,
                    Quantity = 1
                }
            ]
        });

        var createHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Create/{product.Id}"));

        createHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        createHtml.ShouldNotContain("type=\"date\"");

        var cloneModel = new global::VPureLux.Web.Pages.Bom.CloneModel(GetRequiredService<IBomAppService>())
        {
            Id = bom.Id,
            EffectiveFromText = "18/06/2026"
        };

        var result = await cloneModel.OnPostAsync();
        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        var cloneId = redirect.RouteValues!["id"].ShouldBeOfType<Guid>();
        var clone = await GetRequiredService<IBomAppService>().GetAsync(cloneId);

        clone.EffectiveFrom.ShouldBe(new DateTime(2026, 6, 18));
    }

    private async Task<ProductDto> CreateProductAsync(string prefix, string name)
    {
        return await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = prefix + Guid.NewGuid().ToString("N")[..8],
            Name = name
        });
    }

    private async Task<ComponentDto> CreateComponentAsync(string prefix, string name)
    {
        return await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = prefix + Guid.NewGuid().ToString("N")[..8],
            Name = name,
            Unit = "pcs"
        });
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
