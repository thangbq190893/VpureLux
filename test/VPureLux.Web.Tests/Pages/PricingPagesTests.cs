using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Localization;
using VPureLux.Pricing;
using Xunit;
using ComponentCreateModel = VPureLux.Web.Pages.Pricing.Components.CreateModel;
using ProductCreateModel = VPureLux.Web.Pages.Pricing.Products.CreateModel;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class PricingPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Pricing_Index_Should_List_Only_Active_Components_For_Component_Suggested_Prices()
    {
        var componentService = GetRequiredService<IComponentAppService>();
        var active = await componentService.CreateAsync(ComponentInput("PRICE-A", "Active Pricing Component"));
        var inactive = await componentService.CreateAsync(ComponentInput("PRICE-I", "Inactive Pricing Component"));
        await componentService.DeactivateAsync(inactive.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Pricing"));

        html.ShouldContain(active.Name);
        html.ShouldNotContain(inactive.Name);
    }

    [Fact]
    public async Task Component_Price_History_Should_Render_Empty_State_And_Component_Context()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-H", "History Empty Component"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Pricing/Components/{component.Id}"));

        html.ShouldContain(localizer["Pricing:NoVersion"].Value);
        html.ShouldContain($"Linh kiện: {component.Code} - {component.Name}");
    }

    [Fact]
    public async Task Component_Price_Create_Should_Render_Vietnamese_Labels_And_Component_Context()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-C", "Create Price Component"));
        var today = DateTime.Now.Date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Pricing/Components/Create/{component.Id}"));

        html.ShouldContain(localizer["Pricing:SuggestedSellingPrice"].Value);
        html.ShouldContain(localizer["Pricing:Reason"].Value);
        html.ShouldContain(localizer["Pricing:EffectiveFrom"].Value);
        html.ShouldContain($"Linh kiện: {component.Code} - {component.Name}");
        html.ShouldContain($"value=\"{today}\"");
        html.ShouldNotContain("type=\"date\"");
    }

    [Fact]
    public async Task Product_Price_Create_Should_Render_Vietnamese_Date_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(ProductInput("PRICE-P", "Create Price Product"));
        var today = DateTime.Now.Date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Pricing/Products/Create/{product.Id}"));

        html.ShouldContain(localizer["Pricing:SuggestedSellingPrice"].Value);
        html.ShouldContain(localizer["Pricing:EffectiveFrom"].Value);
        html.ShouldContain($"value=\"{today}\"");
        html.ShouldNotContain("type=\"date\"");
    }

    [Fact]
    public async Task Pricing_Create_PageModels_Should_Accept_Vietnamese_Date_Input()
    {
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-PC", "PageModel Component"));
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(ProductInput("PRICE-PP", "PageModel Product"));
        var today = DateTime.Now.Date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        var componentModel = new ComponentCreateModel(
            GetRequiredService<IComponentSuggestedSellingPriceAppService>(),
            GetRequiredService<IComponentAppService>())
        {
            ComponentId = component.Id,
            EffectiveFromText = today,
            Input = new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = 30000m,
                Reason = "Điều chỉnh giá bán đề xuất linh kiện"
            }
        };
        var productModel = new ProductCreateModel(GetRequiredService<IProductSuggestedPriceAppService>())
        {
            ProductId = product.Id,
            EffectiveFromText = today,
            Input = new CreateProductSuggestedPriceVersionDto
            {
                Price = 100000m,
                Reason = "Điều chỉnh giá bán đề xuất sản phẩm"
            }
        };

        (await componentModel.OnPostAsync()).ShouldBeOfType<RedirectToPageResult>();
        (await productModel.OnPostAsync()).ShouldBeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Pricing_Index_Product_Tab_Should_Render_Product_Pricing_Context()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(ProductInput("PRICE-CTX", "Context Product"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Pricing"));

        html.ShouldContain(product.Name);
        html.ShouldContain(localizer["Pricing:BomStatus"].Value);
        html.ShouldContain(localizer["Pricing:ComponentBuildPrice"].Value);
        html.ShouldContain(localizer["Pricing:CurrentProductSuggestedPrice"].Value);
        html.ShouldContain(localizer["Pricing:Difference"].Value);
        html.ShouldContain(localizer["Pricing:NoPublishedBom"].Value);
        html.ShouldContain(localizer["Pricing:NoProductSuggestedPrice"].Value);
    }

    private static CreateComponentDto ComponentInput(string prefix, string name) => new()
    {
        Code = prefix + Guid.NewGuid().ToString("N")[..8],
        Name = name,
        Unit = "pcs"
    };

    private static CreateProductDto ProductInput(string prefix, string name) => new()
    {
        Code = prefix + Guid.NewGuid().ToString("N")[..8],
        Name = name
    };
}
