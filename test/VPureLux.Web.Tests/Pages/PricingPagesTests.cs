using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Localization;
using VPureLux.Pricing;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Xunit;
using ComponentCreateModel = VPureLux.Web.Pages.Pricing.Components.CreateModel;
using PricingIndexModel = VPureLux.Web.Pages.Pricing.IndexModel;
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

        var activeRows = await GetComponentPricingRowsAsync(active.Code);
        var inactiveRows = await GetComponentPricingRowsAsync(inactive.Code);

        activeRows.Items.ShouldContain(x => x.ComponentId == active.Id && x.Name == active.Name);
        inactiveRows.TotalCount.ShouldBe(0);
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

        html.ShouldContain(localizer["Pricing:ProductListPrice"].Value);
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
                Reason = "Điều chỉnh giá niêm yết sản phẩm"
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
        var rows = await GetProductPricingRowsAsync(product.Code);

        html.ShouldNotContain(product.Name);
        html.ShouldContain("id=\"PricingProductsTable\"");
        html.ShouldContain(localizer["Pricing:BomStatus"].Value);
        html.ShouldContain(localizer["Pricing:ComponentBuildPrice"].Value);
        html.ShouldContain(localizer["Pricing:CurrentProductSuggestedPrice"].Value);
        html.ShouldContain(localizer["Pricing:Difference"].Value);
        rows.Items.ShouldContain(x =>
            x.ProductId == product.Id &&
            !x.HasPublishedBom &&
            !x.CurrentProductSuggestedPrice.HasValue &&
            x.CanCreateSuggestedPrice);
    }

    [Fact]
    public async Task Pricing_Index_Product_Tab_Should_Show_Component_Total_As_Reference_And_Product_List_Price()
    {
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-REF-C", "Reference Component"));
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(ProductInput("PRICE-REF-P", "Reference Product"));
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 2 }]
        });
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);
        await GetRequiredService<IComponentSuggestedSellingPriceAppService>()
            .CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
            {
                EffectiveFrom = DateTime.Now.Date,
                Price = 40_000m,
                Reason = "Giá vật tư tham khảo"
            });
        await GetRequiredService<IProductSuggestedPriceAppService>()
            .CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
            {
                EffectiveFrom = DateTime.Now.Date,
                Price = 150_000m,
                Reason = "Giá niêm yết sản phẩm"
            });

        var rows = await GetProductPricingRowsAsync(product.Code);

        rows.Items.ShouldContain(x =>
            x.ProductId == product.Id &&
            x.HasPublishedBom &&
            x.ComponentBuildPrice == 80_000m &&
            x.CurrentProductSuggestedPrice == 150_000m &&
            x.Difference == 70_000m);
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
        var currentRows = await GetComponentPricingRowsAsync(component.Code);
        var noPriceRows = await GetComponentPricingRowsAsync(componentWithoutPrice.Code);

        html.ShouldNotContain(component.Code);
        html.ShouldContain("id=\"PricingComponentsTable\"");
        currentRows.Items.ShouldContain(x =>
            x.ComponentId == component.Id &&
            x.CurrentSuggestedSellingPrice == 123456m &&
            x.EffectiveFrom == effectiveFrom.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")));
        noPriceRows.Items.ShouldContain(x =>
            x.ComponentId == componentWithoutPrice.Id &&
            !x.HasCurrentSuggestedSellingPrice &&
            x.CanCreateSuggestedPrice);
        localizer["Pricing:NoComponentSuggestedPrice"].Value.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Pricing_Index_Component_Tab_Should_Render_Friendly_Empty_State_When_No_Current_Price()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(ComponentInput("PRICE-NO", "No Current Price Component"));

        var rows = await GetComponentPricingRowsAsync(component.Code);

        rows.Items.ShouldContain(x => x.ComponentId == component.Id && !x.CurrentSuggestedSellingPrice.HasValue);
        localizer["Pricing:NoComponentSuggestedPrice"].Value.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Pricing_Index_Should_Use_Abp_DataTables_Server_Paging()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Pricing/Index.cshtml"));
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Pricing/Index.js"));

        pageSource.ShouldContain("<abp-script src=\"/Pages/Pricing/Index.js\" />");
        pageSource.ShouldContain("id=\"PricingComponentsTable\"");
        pageSource.ShouldContain("id=\"PricingProductsTable\"");
        pageSource.ShouldNotContain("@foreach");
        pageSource.ShouldNotContain("Model.Components");
        pageSource.ShouldNotContain("Model.ProductPricingContexts");

        pageModelSource.ShouldContain("OnGetComponentListAsync");
        pageModelSource.ShouldContain("OnGetProductListAsync");
        pageModelSource.ShouldNotContain("MaxMaxResultCount");
        pageModelSource.ShouldNotContain("ProductPricingContexts");

        scriptSource.ShouldContain("DataTable");
        scriptSource.ShouldContain("serverSide: true");
        scriptSource.ShouldContain("handler=ComponentList");
        scriptSource.ShouldContain("handler=ProductList");
        scriptSource.ShouldContain("PricingComponentsClearButton");
        scriptSource.ShouldContain("PricingProductsClearButton");
        scriptSource.ShouldContain("Pricing/Components/Create/");
        scriptSource.ShouldContain("Pricing/Products/Create/");
        scriptSource.ShouldContain("Pricing:CreateNewVersion");
        scriptSource.ShouldNotContain("encode(l(");
        scriptSource.ShouldNotContain("const ");
        scriptSource.ShouldNotContain("let ");
        scriptSource.IndexOf("select2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
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

    private async Task<PagedResultDto<PricingIndexModel.ComponentPricingListRow>> GetComponentPricingRowsAsync(
        string? keyword = null,
        int skipCount = 0,
        int maxResultCount = 10)
    {
        var model = CreatePricingIndexModel();
        var result = await model.OnGetComponentListAsync(new GetComponentListInput
        {
            Keyword = keyword,
            SkipCount = skipCount,
            MaxResultCount = maxResultCount
        });

        return result.Value.ShouldBeOfType<PagedResultDto<PricingIndexModel.ComponentPricingListRow>>();
    }

    private async Task<PagedResultDto<PricingIndexModel.ProductPricingListRow>> GetProductPricingRowsAsync(
        string? keyword = null,
        int skipCount = 0,
        int maxResultCount = 10)
    {
        var model = CreatePricingIndexModel();
        var result = await model.OnGetProductListAsync(new GetProductListInput
        {
            Keyword = keyword,
            SkipCount = skipCount,
            MaxResultCount = maxResultCount
        });

        return result.Value.ShouldBeOfType<PagedResultDto<PricingIndexModel.ProductPricingListRow>>();
    }

    private PricingIndexModel CreatePricingIndexModel()
    {
        var model = new PricingIndexModel(
            GetRequiredService<IComponentAppService>(),
            GetRequiredService<IComponentSuggestedSellingPriceLookupService>(),
            GetRequiredService<IProductAppService>(),
            GetRequiredService<IProductPricingContextLookupService>(),
            GetRequiredService<IAuthorizationService>());
        SetPageContext(model);
        return model;
    }

    private void SetPageContext(PageModel model)
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = GetRequiredService<IServiceProvider>()
            }
        };

        if (model is global::VPureLux.Web.Pages.VPureLuxPageModel vplModel)
        {
            vplModel.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }
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
