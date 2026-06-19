using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Localization;
using VPureLux.Pricing;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Component_Page_Should_Render_Permitted_Actions()
    {
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-ACT"),
            Name = "Action Component",
            Unit = "pcs"
        });
        var response = await GetResponseAsStringAsync("/Catalog/Components");

        response.ShouldContain("/Catalog/Components/Create");
        response.ShouldContain("data-catalog-create");
        response.ShouldContain("dropdown-menu");
    }

    [Fact]
    public async Task Component_Page_Should_Show_Activate_Action_For_Inactive_Component()
    {
        var componentService = GetRequiredService<IComponentAppService>();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var active = await componentService.CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-A"),
            Name = "Active Component",
            Unit = "pcs"
        });
        var inactive = await componentService.CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-I"),
            Name = "Inactive Component",
            Unit = "pcs"
        });
        await componentService.DeactivateAsync(inactive.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Catalog/Components"));

        html.ShouldContain(active.Name);
        html.ShouldContain(inactive.Name);
        html.ShouldContain(localizer["Deactivate"].Value);
        html.ShouldContain(localizer["Activate"].Value);
        html.ShouldContain("handler=Activate");
        html.ShouldContain("data-catalog-status-form");
        html.ShouldContain(localizer["Catalog:ConfirmDeactivateComponent"].Value);
        html.ShouldContain(localizer["Catalog:ConfirmActivateComponent"].Value);
    }

    [Fact]
    public async Task Product_Page_Should_Render_Permitted_Actions()
    {
        await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("PRD-ACT"),
            Name = "Action Product"
        });
        var response = await GetResponseAsStringAsync("/Catalog/Products");

        response.ShouldContain("/Catalog/Products/Create");
        response.ShouldContain("data-catalog-create");
        response.ShouldContain("dropdown-menu");
    }

    [Fact]
    public async Task Product_Page_Should_Render_Action_Menu_Modals_Status_Hooks_And_Context()
    {
        var productService = GetRequiredService<IProductAppService>();
        var priceService = GetRequiredService<IProductSuggestedPriceAppService>();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await productService.CreateAsync(new CreateProductDto
        {
            Code = Unique("CAT-P"),
            Name = "Catalog Product Context"
        });
        await priceService.CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            Price = 88888m,
            Reason = "Catalog list context",
            EffectiveFrom = DateTime.Today
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Catalog/Products"));

        html.ShouldContain(product.Code);
        html.ShouldContain(product.Name);
        html.ShouldContain("data-catalog-index");
        html.ShouldContain("data-create-view-url=\"Catalog/Products/CreateModal\"");
        html.ShouldContain("data-edit-view-url=\"Catalog/Products/EditModal\"");
        html.ShouldContain("data-details-view-url=\"Catalog/Products/DetailsModal\"");
        html.ShouldContain("data-catalog-details");
        html.ShouldContain("data-catalog-edit");
        html.ShouldContain("data-catalog-status-form");
        html.ShouldContain("dropdown-menu");
        html.ShouldContain(localizer["Catalog:ManageImage"].Value);
        html.ShouldContain(localizer["Catalog:CurrentProductSuggestedPrice"].Value);
        html.ShouldContain(88888m.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")));
        html.ShouldContain(localizer["Catalog:NoPublishedBom"].Value);
        html.ShouldContain(localizer["Catalog:ConfirmDeactivateProduct"].Value);
    }

    [Fact]
    public async Task Component_Page_Should_Render_Action_Menu_Modals_Status_Hooks_And_Context()
    {
        var componentService = GetRequiredService<IComponentAppService>();
        var priceService = GetRequiredService<IComponentSuggestedSellingPriceAppService>();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var component = await componentService.CreateAsync(new CreateComponentDto
        {
            Code = Unique("CAT-C"),
            Name = "Catalog Component Context",
            Unit = "pcs"
        });
        await priceService.CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            Price = 45678m,
            Reason = "Catalog component context",
            EffectiveFrom = DateTime.Today
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Catalog/Components"));

        html.ShouldContain(component.Code);
        html.ShouldContain(component.Name);
        html.ShouldContain("data-catalog-index");
        html.ShouldContain("data-create-view-url=\"Catalog/Components/CreateModal\"");
        html.ShouldContain("data-edit-view-url=\"Catalog/Components/EditModal\"");
        html.ShouldContain("data-details-view-url=\"Catalog/Components/DetailsModal\"");
        html.ShouldContain("data-catalog-details");
        html.ShouldContain("data-catalog-edit");
        html.ShouldContain("data-catalog-status-form");
        html.ShouldContain("dropdown-menu");
        html.ShouldContain(localizer["Catalog:ManageImage"].Value);
        html.ShouldContain(localizer["Catalog:CurrentComponentSuggestedPrice"].Value);
        html.ShouldContain(45678m.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")));
        html.ShouldContain(localizer["Catalog:ConfirmDeactivateComponent"].Value);
    }

    [Fact]
    public async Task Catalog_Full_Page_Fallback_Routes_Should_Still_Render()
    {
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CAT-FC"),
            Name = "Fallback Component",
            Unit = "pcs"
        });
        var product = await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("CAT-FP"),
            Name = "Fallback Product"
        });

        foreach (var route in new[]
        {
            "/Catalog/Products/Create",
            $"/Catalog/Products/Edit/{product.Id}",
            $"/Catalog/Products/Details/{product.Id}",
            "/Catalog/Components/Create",
            $"/Catalog/Components/Edit/{component.Id}",
            $"/Catalog/Components/Details/{component.Id}"
        })
        {
            (await GetResponseAsStringAsync(route)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Catalog_Modal_Routes_Should_Render()
    {
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CAT-MC"),
            Name = "Modal Component",
            Unit = "pcs"
        });
        var product = await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("CAT-MP"),
            Name = "Modal Product"
        });

        foreach (var route in new[]
        {
            "/Catalog/Products/CreateModal",
            $"/Catalog/Products/EditModal/{product.Id}",
            $"/Catalog/Products/DetailsModal/{product.Id}",
            "/Catalog/Components/CreateModal",
            $"/Catalog/Components/EditModal/{component.Id}",
            $"/Catalog/Components/DetailsModal/{component.Id}"
        })
        {
            var html = await GetResponseAsStringAsync(route);
            html.ShouldContain("class=\"modal");
            html.ShouldNotContain("<html");
        }
    }

    [Fact]
    public async Task Catalog_Razor_Pages_Should_Stay_Script_Link_And_Action_Compliant()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Products/Create.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Products/Edit.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Products/Details.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/Create.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/Edit.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/Details.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldNotContain("<abp-button href=");
            pageSource.ShouldNotContain("href=\"/");
            pageSource.ShouldNotContain("<script>");
            pageSource.ShouldNotContain("<script src=");
        }

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/CatalogIndex.js"));
        scriptSource.ShouldContain("new abp.ModalManager");
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.confirmed");
    }

    [Fact]
    public async Task Catalog_Api_Should_Use_Documented_Route_And_Response_Wrapper()
    {
        var response = await GetResponseAsStringAsync("/api/catalog/components?page=1&pageSize=10");

        response.ShouldContain("\"success\":true");
        response.ShouldContain("\"data\":");
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
