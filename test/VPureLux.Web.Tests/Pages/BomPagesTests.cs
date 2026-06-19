using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Localization;
using VPureLux.Pricing;
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
        scriptSource.ShouldContain("component.name = 'Items[' + index + '].ComponentId'");
        scriptSource.ShouldContain("quantity.name = 'Items[' + index + '].Quantity'");
        scriptSource.ShouldContain("component.id = 'Items_' + index + '__ComponentId'");
        scriptSource.ShouldContain("quantity.id = 'Items_' + index + '__Quantity'");
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
    public async Task Bom_Product_Should_Render_Publish_Archive_Confirmation_And_Notification_Hooks()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("BOM-ACT-P", "BOM Action Product");
        var component = await CreateComponentAsync("BOM-ACT-C", "BOM Action Component");
        var bomService = GetRequiredService<IBomAppService>();
        var draft = await bomService.CreateAsync(product.Id, BomInput(component.Id, DateTime.Today));

        var draftHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        draftHtml.ShouldContain("data-bom-product");
        draftHtml.ShouldContain("data-bom-action-form");
        draftHtml.ShouldContain(localizer["Bom:ConfirmPublish"].Value);

        await bomService.PublishAsync(draft.Id);
        var publishedHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        publishedHtml.ShouldContain(localizer["Bom:ConfirmArchive"].Value);

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Product.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Bom/BomProduct.js\" />");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
        pageSource.ShouldNotContain("<abp-button href=");
        pageSource.ShouldNotContain("href=\"/");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/BomProduct.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.confirmed");
    }

    [Fact]
    public async Task Bom_Product_Should_Render_Product_And_Existing_Pricing_Context()
    {
        var product = await CreateProductAsync("BOM-CTX-P", "BOM Context Product");
        var component = await CreateComponentAsync("BOM-CTX-C", "BOM Context Component");
        await GetRequiredService<IComponentSuggestedSellingPriceAppService>()
            .CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = 50000m,
                Reason = "Giá linh kiện cho ngữ cảnh BOM",
                EffectiveFrom = DateTime.Today
            });
        await GetRequiredService<IProductSuggestedPriceAppService>()
            .CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
            {
                Price = 120000m,
                Reason = "Giá sản phẩm cho ngữ cảnh BOM",
                EffectiveFrom = DateTime.Today
            });

        var bom = await GetRequiredService<IBomAppService>().CreateAsync(
            product.Id,
            BomInput(component.Id, DateTime.Today, quantity: 2));
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));

        html.ShouldContain($"Sản phẩm: {product.Code} - {product.Name}");
        html.ShouldContain("Giá bán đề xuất hiện tại");
        html.ShouldContain("Giá cấu thành linh kiện");
        html.ShouldContain("120.000 VND");
        html.ShouldContain("100.000 VND");
        html.ShouldContain("Đã có định mức công bố");
    }

    [Fact]
    public async Task Bom_Edit_Should_Preserve_Component_Selections_And_Quantities()
    {
        var product = await CreateProductAsync("BOM-EDIT-P", "BOM Edit Product");
        var firstComponent = await CreateComponentAsync("BOM-EDIT-C1", "BOM Edit Component 1");
        var secondComponent = await CreateComponentAsync("BOM-EDIT-C2", "BOM Edit Component 2");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items =
            [
                new CreateBomItemDto { ComponentId = firstComponent.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = secondComponent.Id, Quantity = 3 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Edit/{bom.Id}"));

        html.ShouldContain($"{firstComponent.Code} - {firstComponent.Name}");
        html.ShouldContain($"{secondComponent.Code} - {secondComponent.Name}");
        html.ShouldContain("name=\"Items[0].ComponentId\"");
        html.ShouldContain("name=\"Items[1].ComponentId\"");
        html.ShouldContain("name=\"Items[0].Quantity\"");
        html.ShouldContain("name=\"Items[1].Quantity\"");
        html.ShouldContain("value=\"2\"");
        html.ShouldContain("value=\"3\"");
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

    private static CreateBomVersionDto BomInput(Guid componentId, DateTime effectiveFrom, decimal quantity = 1) => new()
    {
        EffectiveFrom = effectiveFrom,
        Items =
        [
            new CreateBomItemDto
            {
                ComponentId = componentId,
                Quantity = quantity
            }
        ]
    };

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
