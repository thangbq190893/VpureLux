using System;
using System.Globalization;
using System.IO;
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
        html.ShouldContain($"Vật tư: {component.Code} - {component.Name}");
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
        html.ShouldContain($"Vật tư: {component.Code} - {component.Name}");
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
        html.ShouldContain($"Sản phẩm: {product.Code} - {product.Name}");
        html.ShouldContain($"value=\"{today}\"");
        html.ShouldNotContain("type=\"date\"");
    }

    [Fact]
    public async Task Product_Price_History_Should_Render_Empty_State_And_Product_Context()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(ProductInput("PRICE-PH", "History Empty Product"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Pricing/Products/{product.Id}"));

        html.ShouldContain(localizer["Pricing:NoVersion"].Value);
        html.ShouldContain($"Sản phẩm: {product.Code} - {product.Name}");
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
                Reason = "Điều chỉnh giá bán đề xuất vật tư"
            }
        };
        var productModel = new ProductCreateModel(
            GetRequiredService<IProductSuggestedPriceAppService>(),
            GetRequiredService<IProductAppService>())
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

    [Fact]
    public async Task Pricing_Index_Component_Tab_Should_Render_Current_Suggested_Price_And_Effective_Date()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-CUR", "Current Price Component"));
        var componentWithoutPrice = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-CNP", "No Batch Price Component"));
        var effectiveFrom = DateTime.Now.Date;
        await GetRequiredService<IComponentSuggestedSellingPriceAppService>()
            .CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = 123456m,
                Reason = "Giá bán đề xuất hiện tại",
                EffectiveFrom = effectiveFrom
            });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Pricing"));

        html.ShouldContain(component.Code);
        html.ShouldContain(component.Name);
        html.ShouldContain(componentWithoutPrice.Code);
        html.ShouldContain(componentWithoutPrice.Name);
        html.ShouldContain($"{123456m.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} VND");
        html.ShouldContain(effectiveFrom.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")));
        html.ShouldContain(localizer["Pricing:NoComponentSuggestedPrice"].Value);
    }

    [Fact]
    public async Task Pricing_Index_Component_Tab_Should_Render_Friendly_Empty_State_When_No_Current_Price()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-NO", "No Current Price Component"));

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Pricing"));

        html.ShouldContain(component.Name);
        html.ShouldContain(localizer["Pricing:NoComponentSuggestedPrice"].Value);
    }

    [Fact]
    public async Task Pricing_Razor_Pages_Should_Stay_Script_Link_And_Raw_Id_Compliant()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Pricing/Index.cshtml",
            "src/VPureLux.Web/Pages/Pricing/Components/Create.cshtml",
            "src/VPureLux.Web/Pages/Pricing/Components/History.cshtml",
            "src/VPureLux.Web/Pages/Pricing/Products/Create.cshtml",
            "src/VPureLux.Web/Pages/Pricing/Products/History.cshtml"
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
    public async Task Pricing_Index_PageModel_Should_Use_Batch_Current_Price_Lookup()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs"));

        pageSource.ShouldContain("FindCurrentMapAsync");
        pageSource.ShouldNotContain("GetCurrentAsync(");
        pageSource.ShouldNotContain("TryGetCurrentComponentPriceAsync");
    }

    [Fact]
    public async Task Product_Pricing_Context_Should_Use_Batch_Reads()
    {
        var appServiceSource = await File.ReadAllTextAsync(GetRepoFilePath(
            "src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs"));
        var lookupSource = await File.ReadAllTextAsync(GetRepoFilePath(
            "src/VPureLux.Application/Pricing/ProductPricingContextLookupService.cs"));

        appServiceSource.ShouldNotContain("foreach (var product");
        lookupSource.ShouldContain("FindAtDateMapAsync(productIds");
        lookupSource.ShouldContain("GetPublishedMapByProductIdsAsync(productIds");
        lookupSource.ShouldContain("FindCurrentMapAsync(componentIds");
        lookupSource.ShouldNotContain("FindAtDateAsync(product.Id");
        lookupSource.ShouldNotContain("GetListByProductIdAsync(product.Id");
        lookupSource.ShouldNotContain("FindAtDateAsync(item.ComponentId");
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
