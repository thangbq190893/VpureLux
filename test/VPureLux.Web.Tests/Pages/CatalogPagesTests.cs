using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux;
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
        response.ShouldContain("id=\"ComponentsTable\"");
        response.ShouldContain("data-table-selector=\"#ComponentsTable\"");
    }

    [Fact]
    public async Task Component_Index_DataTable_Should_Keep_Status_Actions()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath(
            "src/VPureLux.Web/Pages/Catalog/Components/Index.js"));

        scriptSource.ShouldContain("rowAction");
        scriptSource.ShouldContain("l('Deactivate')");
        scriptSource.ShouldContain("l('Activate')");
        scriptSource.ShouldContain("handler=' + handler");
        scriptSource.ShouldContain("Catalog:ConfirmDeactivateComponent");
        scriptSource.ShouldContain("Catalog:ConfirmActivateComponent");
        scriptSource.ShouldContain("dataTable.ajax.reload(null, false)");
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
        html.ShouldContain(FormatVnd(88888m));
        html.ShouldNotContain("88888.000000");
        html.ShouldContain(localizer["Catalog:NoPublishedBom"].Value);
        html.ShouldContain(localizer["Catalog:ConfirmDeactivateProduct"].Value);
    }

    [Fact]
    public async Task Component_Page_Should_Render_DataTables_Modals_Status_Hooks_And_Context()
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
        var componentWithoutPrice = await componentService.CreateAsync(new CreateComponentDto
        {
            Code = Unique("CAT-CNP"),
            Name = "Catalog Component Without Price",
            Unit = "pcs"
        });
        await priceService.CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            Price = 45678m,
            Reason = "Catalog component context",
            EffectiveFrom = DateTime.Today
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Catalog/Components"));

        html.ShouldNotContain(component.Code);
        html.ShouldNotContain(componentWithoutPrice.Code);
        html.ShouldContain("data-catalog-index");
        html.ShouldContain("id=\"ComponentsTable\"");
        html.ShouldContain("data-create-view-url=\"Catalog/Components/CreateModal\"");
        html.ShouldContain("data-edit-view-url=\"Catalog/Components/EditModal\"");
        html.ShouldContain("data-details-view-url=\"Catalog/Components/DetailsModal\"");
        html.ShouldContain("data-table-selector=\"#ComponentsTable\"");
        html.ShouldContain("data-can-edit=\"true\"");
        html.ShouldContain("data-can-view-pricing-context=\"true\"");
        html.ShouldContain(localizer["Catalog:CurrentComponentSuggestedPrice"].Value);

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Components/Index.js"));
        scriptSource.ShouldContain("Catalog:ManageImage");
        scriptSource.ShouldContain("Catalog:NoComponentSuggestedPrice");
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
    public async Task Catalog_Modal_Routes_Should_Render_With_Path_And_Query_Id()
    {
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CAT-MQ"),
            Name = "Modal Query Component",
            Unit = "pcs"
        });
        var product = await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("CAT-MPQ"),
            Name = "Modal Query Product"
        });

        foreach (var route in new[]
        {
            $"/Catalog/Products/DetailsModal/{product.Id}",
            $"/Catalog/Products/DetailsModal?id={product.Id}",
            $"/Catalog/Products/EditModal/{product.Id}",
            $"/Catalog/Products/EditModal?id={product.Id}",
            $"/Catalog/Components/DetailsModal/{component.Id}",
            $"/Catalog/Components/DetailsModal?id={component.Id}",
            $"/Catalog/Components/EditModal/{component.Id}",
            $"/Catalog/Components/EditModal?id={component.Id}"
        })
        {
            var html = await GetResponseAsStringAsync(route);
            html.ShouldContain("class=\"modal");
            html.ShouldNotContain("<html");
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
            $"/Catalog/Products/EditModal?id={product.Id}",
            $"/Catalog/Products/DetailsModal?id={product.Id}",
            "/Catalog/Components/CreateModal",
            $"/Catalog/Components/EditModal?id={component.Id}",
            $"/Catalog/Components/DetailsModal?id={component.Id}"
        })
        {
            var html = await GetResponseAsStringAsync(route);
            html.ShouldContain("class=\"modal");
            html.ShouldNotContain("<html");
        }
    }

    [Fact]
    public async Task Product_Create_Pages_Should_Render_Manual_Required_Code_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        foreach (var route in new[] { "/Catalog/Products/Create", "/Catalog/Products/CreateModal" })
        {
            var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(route));

            html.ShouldContain(localizer["Catalog:ProductCode"].Value);
            html.ShouldContain("name=\"Input.Code\"");
            html.ShouldContain("data-val=\"true\"");
            html.ShouldContain("Vui lòng nhập mã sản phẩm.");
            html.ShouldNotContain(localizer["Catalog:CodeAutoGeneratedOnSave"].Value);
        }
    }

    [Fact]
    public async Task Product_Code_Manual_Input_Should_Not_Use_Auto_Generator_And_Edit_Remains_Readonly()
    {
        var productCreatePage = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Products/Create.cshtml"));
        var productCreateModal = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Products/CreateModal.cshtml"));
        var productEditPage = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Products/Edit.cshtml"));
        var productAppService = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Application/Catalog/Products/ProductAppService.cs"));
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        productCreatePage.ShouldContain("asp-for=\"Input.Code\"");
        productCreateModal.ShouldContain("asp-for=\"Input.Code\"");
        productCreatePage.ShouldNotContain("Catalog:CodeAutoGeneratedOnSave");
        productCreateModal.ShouldNotContain("Catalog:CodeAutoGeneratedOnSave");
        productEditPage.ShouldContain("<abp-input asp-for=\"Code\" disabled=\"true\" />");
        productAppService.ShouldContain("ResolveManualCode");
        productAppService.ShouldNotContain("IBusinessCodeGenerator");
        productAppService.ShouldNotContain("ProductPrefix");
        productAppService.ShouldNotContain("GenerateAsync");
        localizer[VPureLuxDomainErrorCodes.ProductCodeRequired].Value.ShouldBe("Vui lòng nhập mã sản phẩm.");
        localizer[VPureLuxDomainErrorCodes.ProductCodeAlreadyExists].Value.ShouldBe("Mã sản phẩm đã tồn tại.");
    }

    [Fact]
    public async Task Catalog_Edit_Readonly_Code_Should_Not_Bind_On_Post()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Catalog/Products/Edit.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Products/EditModal.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/Edit.cshtml",
            "src/VPureLux.Web/Pages/Catalog/Components/EditModal.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldContain("<abp-input asp-for=\"Code\" disabled=\"true\" />");
        }

        foreach (var modelType in new[]
        {
            typeof(Web.Pages.Catalog.Products.EditModel),
            typeof(Web.Pages.Catalog.Products.EditModalModel),
            typeof(Web.Pages.Catalog.Components.EditModel),
            typeof(Web.Pages.Catalog.Components.EditModalModel)
        })
        {
            modelType.GetProperty("Code")!
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.BindPropertyAttribute), inherit: true)
                .ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Component_Create_Pages_Should_Show_Auto_Code_Hint_And_Not_Post_Code_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        foreach (var route in new[] { "/Catalog/Components/Create", "/Catalog/Components/CreateModal" })
        {
            var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(route));

            html.ShouldContain(localizer["Catalog:CodeAutoGeneratedOnSave"].Value);
            html.ShouldNotContain("name=\"Input.Code\"");
        }
    }

    [Fact]
    public async Task Component_Index_Should_Use_Abp_DataTables_Server_Paging_And_Newest_First()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml"));
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/Components/Index.js"));
        var catalogScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Catalog/CatalogIndex.js"));
        var appServiceSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Application/Catalog/Components/ComponentAppService.cs"));

        pageSource.ShouldContain("<abp-table id=\"ComponentsTable\"");
        pageSource.ShouldNotContain("foreach (var row in Model.Components)");
        pageSource.ShouldNotContain("method=\"get\"");
        pageModelSource.ShouldContain("public async Task OnGetAsync()");
        pageModelSource.ShouldContain("await SetPermissionsAsync();");
        pageModelSource.ShouldContain("OnGetListAsync(GetComponentListInput input)");
        pageModelSource.ShouldContain("_componentAppService.GetListAsync");
        pageModelSource.ShouldContain("FindCurrentMapAsync");
        pageModelSource.ShouldNotContain("MaxResultCount = 100");

        scriptSource.ShouldContain("$(tableSelector).DataTable");
        scriptSource.ShouldContain("abp.libs.datatables.normalizeConfiguration");
        scriptSource.ShouldContain("serverSide: true");
        scriptSource.ShouldContain("paging: true");
        scriptSource.ShouldContain("searching: false");
        scriptSource.ShouldContain("abp.libs.datatables.createAjax");
        scriptSource.ShouldContain("handler=List");
        scriptSource.ShouldContain("ComponentsSearchForm");
        scriptSource.ShouldContain("event.preventDefault()");
        scriptSource.ShouldContain("ComponentsClearButton");
        scriptSource.ShouldContain("dataTable.ajax.reload()");
        scriptSource.ShouldContain("rowAction");
        scriptSource.ShouldContain("function recordOf(data)");
        scriptSource.ShouldContain("data?.record || data || {}");
        scriptSource.IndexOf("select2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);

        catalogScriptSource.ShouldContain("page.dataset.tableSelector");
        catalogScriptSource.ShouldContain("DataTable().ajax.reload(null, false)");
        appServiceSource.ShouldContain("DefaultSorting = \"CreationTime DESC\"");
        appServiceSource.ShouldContain("ApplySorting(queryable, input.Sorting)");
        appServiceSource.ShouldContain("OrderByDescending(x => x.CreationTime)");
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
    public async Task Catalog_Component_PageModel_Should_Use_Batch_Current_Price_Lookup()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(
            "src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs"));

        pageSource.ShouldContain("FindCurrentMapAsync");
        pageSource.ShouldNotContain("GetCurrentAsync(");
        pageSource.ShouldNotContain("TryGetCurrentComponentPriceAsync");
    }

    [Fact]
    public async Task Catalog_Product_PageModel_Should_Use_Scoped_Product_Pricing_Context()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(
            "src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml.cs"));

        pageSource.ShouldContain("FindMapAsync");
        pageSource.ShouldNotContain("_productPricingContextAppService.GetListAsync()");
    }

    [Fact]
    public async Task Catalog_Api_Should_Use_Documented_Route_And_Response_Wrapper()
    {
        var response = await GetResponseAsStringAsync("/api/catalog/components?page=1&pageSize=10");

        response.ShouldContain("\"success\":true");
        response.ShouldContain("\"data\":");
    }

    private static string FormatVnd(decimal value)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        return decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("#,0", vi) + " ₫";
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
