using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Localization;
using VPureLux.Pricing;
using VPureLux.Sales;
using VPureLux.Web.Pages.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Uow;
using Xunit;
using CreateModel = VPureLux.Web.Pages.Sales.CreateModel;
using DetailsModel = VPureLux.Web.Pages.Sales.DetailsModel;
using EditModel = VPureLux.Web.Pages.Sales.EditModel;
using IndexModel = VPureLux.Web.Pages.Sales.IndexModel;
using SalesProductContextViewModel = VPureLux.Web.Pages.Sales.SalesProductContextViewModel;
using SalesUiFormatter = VPureLux.Web.Pages.Sales.SalesUiFormatter;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Sales_Index_Create_History_And_Customer_History_Pages_Should_Render()
    {
        foreach (var route in new[] { "/Sales", "/Sales/Create", "/Sales/History", "/Sales/CustomerHistory" })
        {
            (await GetResponseAsStringAsync(route)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Sales_Index_Actions_Should_Be_Permission_Aware()
    {
        var service = Substitute.For<ISalesOrderAppService>();
        service.GetListAsync(Arg.Any<GetSalesOrderListInput>())
            .Returns(new PagedResultDto<SalesOrderDto>());
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        var model = new IndexModel(service, authorization)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } }
        };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanViewHistory.ShouldBeFalse();
    }

    [Fact]
    public async Task Sales_Create_Should_Render_Product_Context_Hooks_And_External_Script()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("SALES-CRT", "Sales Create Product");
        await PublishBomAsync(product.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Sales/Create"));

        html.ShouldContain($"{product.Code} - {product.Name}");
        html.ShouldContain("data-sales-product-select");
        html.ShouldContain("data-sales-product-context");
        html.ShouldContain("data-sales-stock-availability");
        html.ShouldContain("data-sales-context-endpoint");
        html.ShouldContain("data-sales-availability-endpoint");
        html.ShouldContain(localizer["Sales:SelectProductForContext"].Value);
        html.ShouldNotContain("value=\"0,00\"");
        html.ShouldNotContain("value='0,00'");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Sales/SalesProductContext.js\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Sales/SalesCreateLines.js\" />");
        pageSource.ShouldContain("<abp-style src=\"/Pages/Sales/Create.css\" />");
        pageSource.ShouldContain("data-sales-line-row");
        pageSource.ShouldContain("data-sales-product-select");
        pageSource.ShouldContain("data-sales-stock-availability");
        pageSource.ShouldContain("data-sales-availability-endpoint");
        pageSource.ShouldContain("id=\"sales-line-row-template\"");
        pageSource.ShouldContain("<template id=\"sales-line-row-template\">");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ProductId\"");
        pageSource.ShouldContain("id=\"add-sales-line\"");
        pageSource.ShouldContain("remove-sales-line");
        pageSource.ShouldContain("Input.Lines[i].ProductId");
        pageSource.ShouldContain("@for (var i = 0; i < Model.Input.Lines.Count; i++)");
        pageSource.ShouldNotContain("Input.Lines[0].ProductId");
        pageSource.ShouldContain("data-sales-line-index");
        pageSource.ShouldContain("sales-create-lines-editor");
        pageSource.ShouldContain("sales-create-lines-table");
        pageSource.ShouldContain("sales-line-context");
        pageSource.ShouldContain("sales-create-actions");
        pageSource.ShouldContain("sales-action-button");
        pageSource.ShouldContain("btn btn-outline-secondary btn-sm sales-action-button");
        pageSource.ShouldContain("btn btn-primary btn-sm sales-action-button");
        pageSource.ShouldContain("btn btn-secondary btn-sm sales-action-button");
        pageSource.ShouldContain("remove-sales-line sales-action-button");
        pageSource.ShouldContain("data-sales-lines-body");
        pageSource.ShouldContain("sales-line-col-product");
        pageSource.ShouldContain("sales-line-col-status");
        pageSource.ShouldContain("form-select-sm");
        pageSource.ShouldContain("form-control-sm");
        pageSource.ShouldNotContain("alert alert-light border mb-3");
        pageSource.ShouldContain("type=\"number\"");
        pageSource.ShouldContain("Quantity == 0 ? \"1\"");
        pageSource.ShouldContain("value=\"1\"");
        pageSource.ShouldNotContain("select2-container");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
        pageSource.ShouldNotContain("<abp-button href=");
        pageSource.ShouldNotContain("href=\"/");

        var linesScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesCreateLines.js"));
        linesScriptSource.ShouldContain("sales-line-row-template");
        linesScriptSource.ShouldContain("prepareLineRow");
        linesScriptSource.ShouldContain("bootExistingRows");
        linesScriptSource.ShouldContain("whenAbpDomReady");
        linesScriptSource.ShouldContain("cloneTemplateRow");
        linesScriptSource.ShouldContain("data-sales-product-select");
        linesScriptSource.ShouldContain("indexToken");
        linesScriptSource.ShouldContain("applyTemplateAttribute");
        linesScriptSource.ShouldContain("data-name");
        linesScriptSource.ShouldContain("ensureNativeProductSelect");
        linesScriptSource.ShouldContain("stripSelect2Enhancements");
        linesScriptSource.ShouldContain("defaultQuantity");
        linesScriptSource.ShouldContain("productContext.initializeRow");
        linesScriptSource.ShouldNotContain("initializeSelects");
        linesScriptSource.ShouldNotContain("ensureTemplate");
        linesScriptSource.ShouldNotContain("sourceProduct.innerHTML");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));
        scriptSource.ShouldContain("badge bg-success");
        scriptSource.ShouldContain("Sales:PublishedBomAvailable");
        scriptSource.ShouldContain("Sales:HasProductImage");
        scriptSource.ShouldContain(".catch(function ()");
        scriptSource.ShouldContain("getProductSelector");
        scriptSource.ShouldContain("data-sales-product-select");
        scriptSource.ShouldContain("sales-line-row-template");
        scriptSource.ShouldContain("SalesCreatePage");
        scriptSource.ShouldContain("initializeRow");
        scriptSource.ShouldNotContain("ProductLabel");
        scriptSource.ShouldNotContain("fw-semibold mb-1");
        scriptSource.ShouldNotContain("const productSelector = page.querySelector('[data-sales-product-selector]')");
        scriptSource.ShouldNotContain("page.querySelector('[data-sales-product-context]')");
        scriptSource.ShouldContain("getProductContextMap");
        scriptSource.ShouldContain("validateAllRows");
        scriptSource.ShouldContain("validateAllRowsAsync");
        scriptSource.ShouldContain("data-sales-product-eligibility");
        scriptSource.ShouldContain("data-sales-stock-availability");
        scriptSource.ShouldContain("loadProductContextFromMap");
        scriptSource.ShouldContain("Sales:ProductStockSaleNotSupported");

        pageSource.ShouldContain("sales-product-context-data");
        pageSource.ShouldContain("GetProductContextsJson");
        pageSource.ShouldContain("data-sales-product-eligibility");
        pageSource.ShouldContain("data-sales-product-not-eligible");
        pageSource.ShouldContain("data-sales-override-reason-required");
        pageSource.ShouldContain("data-sales-create-alert");
        pageSource.ShouldContain("data-sales-override-validation");
        pageSource.ShouldContain("asp-validation-for=\"Input.Lines[i].ProductId\"");

        linesScriptSource.ShouldContain("getLinesBody");
        linesScriptSource.ShouldContain("data-sales-lines-body");
        linesScriptSource.ShouldContain("validateAllRows");
        linesScriptSource.ShouldContain("data-sales-override-validation");
        linesScriptSource.ShouldContain("validateAllRowsAsync");
        linesScriptSource.ShouldContain("form.dataset.salesValidatedSubmit");
        linesScriptSource.ShouldContain("refreshStockAvailability(container)");
        linesScriptSource.ShouldContain("data-sales-stock-availability");

        scriptSource.ShouldContain("createPage");
        scriptSource.ShouldContain("validateOverrideReason");
        scriptSource.ShouldContain("getOverrideReasonRequiredMessage");
        scriptSource.ShouldContain("showCreateAlert");
        scriptSource.ShouldContain("data-sales-create-alert");
        scriptSource.ShouldContain("data-sales-override-validation");
        scriptSource.ShouldContain("Sales:ManualPriceRequired");
        scriptSource.ShouldContain("Sales:NoSuggestedPriceManualPriceRequired");
        scriptSource.ShouldContain("Sales:StockAvailabilityPreviewDeferred");
        scriptSource.ShouldContain("Sales:AvailableToSellAtWarehouse");
        scriptSource.ShouldContain("Sales:InsufficientStockForRequestedQuantity");
        scriptSource.ShouldContain("Sales:MissingComponentStock");
        scriptSource.ShouldContain("Sales:NoBomStockAvailabilityUnavailable");
        scriptSource.ShouldContain("Sales:StockIssueGlobal");
        scriptSource.ShouldContain("refreshStockAvailability");
        scriptSource.ShouldContain("collectAvailabilityLines");
        scriptSource.ShouldContain("buildAvailabilityUrl");
        scriptSource.ShouldContain("bindWarehouseSelector");
        scriptSource.ShouldContain("bindQuantityInput");
        scriptSource.ShouldContain("salesPriceAutoFilled");
        scriptSource.ShouldContain("salesPreviousProductId");
        scriptSource.ShouldContain("SuggestedPrice");

        var createCss = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.css"));
        createCss.ShouldContain(".sales-create-lines-table");
        createCss.ShouldContain("min-width: 1400px");
        createCss.ShouldContain(".sales-line-col-product");
        createCss.ShouldContain("min-width: 32rem");
        createCss.ShouldContain(".sales-line-product");
        createCss.ShouldContain(".sales-line-col-product .custom-select-wrapper");
        createCss.ShouldContain("min-width: 30rem");
        createCss.ShouldContain(".sales-create-actions");
        createCss.ShouldContain(".sales-action-button");
        createCss.ShouldContain(".sales-line-stock");
    }

    [Fact]
    public async Task Sales_Create_Should_Render_First_Row_Bindings_And_Template_Hooks()
    {
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Sales/Create"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.cshtml"));

        html.ShouldContain("name=\"Input.Lines[0].ProductId\"");
        html.ShouldContain("name=\"Input.Lines[0].Quantity\"");
        html.ShouldContain("name=\"Input.Lines[0].ActualSellingPrice\"");
        html.ShouldContain("name=\"Input.Lines[0].OverrideReason\"");
        html.ShouldContain("data-sales-line-row");
        html.ShouldContain("data-sales-lines-body");
        html.ShouldContain("data-sales-product-select");
        html.ShouldContain("data-sales-product-context");
        html.ShouldContain("data-sales-stock-availability");
        html.ShouldContain("data-sales-actual-price");
        html.ShouldContain("data-sales-override-validation");
        html.ShouldContain("remove-sales-line");

        pageSource.ShouldContain("<template id=\"sales-line-row-template\">");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ProductId\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].Quantity\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ActualSellingPrice\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].OverrideReason\"");
    }

    [Fact]
    public async Task Sales_Create_Lines_Script_Should_Guard_Add_Remove_And_Reindex_Behavior()
    {
        var linesScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesCreateLines.js"));

        linesScriptSource.ShouldContain("function reindexRows(container)");
        linesScriptSource.ShouldContain("row.setAttribute('data-sales-line-index', String(index))");
        linesScriptSource.ShouldContain("applyTemplateAttribute(element, 'data-name', index)");
        linesScriptSource.ShouldContain("applyTemplateAttribute(element, 'data-id', index)");
        linesScriptSource.ShouldContain("addButton.addEventListener('click'");
        linesScriptSource.ShouldContain("linesBody.appendChild(row)");
        linesScriptSource.ShouldContain("reindexRows(container);");
        linesScriptSource.ShouldContain("container.addEventListener('click'");
        linesScriptSource.ShouldContain("event.target.closest('.remove-sales-line')");
        linesScriptSource.ShouldContain("getLiveRows(container).length > 1");
        linesScriptSource.ShouldContain("row.remove();");
        CountOccurrences(linesScriptSource, "refreshStockAvailability(container)").ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Sales_Create_Product_Change_To_Suggested_Product_Should_Reset_Actual_Price()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));

        scriptSource.ShouldContain("function hasSelectedProductChanged(productSelector)");
        scriptSource.ShouldContain("setPreviousProductId(productSelector, selectedProductId)");
        scriptSource.ShouldContain("loadProductContext(scope, { resetPricing: !!createPage && productChanged })");
        scriptSource.ShouldContain("function resetLinePricingForProductChange(scope, data)");
        scriptSource.ShouldContain("actualPriceInput.value = suggestedPrice");
        scriptSource.ShouldContain("markActualPriceAutoFilled(actualPriceInput, true)");
    }

    [Fact]
    public async Task Sales_Create_Product_Change_Should_Clear_Override_Reason_And_Validation()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));
        var linesScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesCreateLines.js"));

        scriptSource.ShouldContain("function clearOverrideReason(scope)");
        scriptSource.ShouldContain("clearOverrideReason(scope);");
        scriptSource.ShouldContain("clearOverrideValidation(scope);");
        linesScriptSource.ShouldContain("delete product.dataset.salesPreviousProductId");
        linesScriptSource.ShouldContain("delete actualPrice.dataset.salesPriceAutoFilled");
    }

    [Fact]
    public async Task Sales_Create_Product_Change_To_Missing_Suggested_Price_Should_Clear_Actual_Price()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        scriptSource.ShouldContain("if (suggestedPrice === null || suggestedPrice === undefined)");
        scriptSource.ShouldContain("actualPriceInput.value = ''");
        scriptSource.ShouldContain("markActualPriceAutoFilled(actualPriceInput, false)");
        scriptSource.ShouldContain("return getNoSuggestedPriceManualMessage();");
        scriptSource.ShouldContain("if (suggestedPrice === null)");
        localizer["Sales:NoSuggestedPriceManualPriceRequired"].Value
            .ShouldBe("Chưa có giá bán đề xuất, cần nhập giá bán thực tế.");
        localizer["Sales:NoSuggestedPriceManualPriceRequired"].Value.ShouldNotContain("không thể bán");
    }

    [Fact]
    public async Task Sales_Create_Manual_Actual_Price_Should_Be_Preserved_While_Product_Stays_Same()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));

        scriptSource.ShouldContain("options.resetPricing");
        scriptSource.ShouldContain("} else if (actualPriceInput && !actualPriceInput.value && suggestedPrice !== null && suggestedPrice !== undefined) {");
        scriptSource.ShouldContain("actualPriceInput._vplSalesActualPriceInputHandler = function ()");
        scriptSource.ShouldContain("markActualPriceAutoFilled(actualPriceInput, false)");
    }

    [Fact]
    public async Task Sales_Create_Manual_Actual_Price_Should_Not_Be_Preserved_After_Product_Changes()
    {
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));

        scriptSource.ShouldContain("var productChanged = hasSelectedProductChanged(productSelector);");
        scriptSource.ShouldContain("loadProductContext(scope, { resetPricing: !!createPage && productChanged })");
        scriptSource.ShouldContain("resetLinePricingForProductChange(scope, data);");
        scriptSource.ShouldContain("resetLinePricingForProductChange(scope, null);");
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Return_Page_When_Product_Has_No_Published_Bom()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-NOBOM");
        var ineligibleProduct = await CreateProductAsync("SALES-NOBOM-P", "No BOM Product");
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = context.CustomerId;
        model.Input.WarehouseId = context.WarehouseId;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = ineligibleProduct.Id,
                Quantity = 1,
                ActualSellingPrice = 100
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"Input.Lines[0].ProductId"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer["Sales:ProductStockSaleNotSupported"].Value);
        localizer["Sales:ProductStockSaleNotSupported"].Value.ShouldContain("tồn kho thành phẩm");
        localizer["Sales:ProductStockSaleNotSupported"].Value.ShouldNotContain("giá bán");
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Block_Override_Without_Reason()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-OR");
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = context.CustomerId;
        model.Input.WarehouseId = context.WarehouseId;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = context.ProductId,
                Quantity = 1,
                ActualSellingPrice = 90
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"Input.Lines[0].OverrideReason"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Save_When_Actual_Price_Equals_Suggested_Without_Reason()
    {
        var context = await CreateSalesContextAsync("SALES-EQ");
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = context.CustomerId;
        model.Input.WarehouseId = context.WarehouseId;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = context.ProductId,
                Quantity = 1,
                ActualSellingPrice = 100
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.PageName.ShouldBe("/Sales/Details");
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Save_Missing_Suggested_Price_With_Manual_Actual_Price()
    {
        var group = await GetRequiredService<ICustomerGroupAppService>()
            .CreateAsync(new CreateCustomerGroupDto { Code = Unique("SNP-G"), Name = "No Suggested Price Group" });
        var customer = await GetRequiredService<ICustomerAppService>()
            .CreateAsync(new CreateCustomerDto { Code = Unique("SNP-C"), Name = "No Suggested Price Customer", CustomerGroupId = group.Id });
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SNP-W"), Name = "No Suggested Price Warehouse" });
        var product = await CreateProductAsync("SNP-P", "No Suggested Price Product");
        var componentId = await PublishBomAsync(product.Id);
        await PostComponentReceiptAsync(warehouse.Id, componentId, 5);
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = customer.Id;
        model.Input.WarehouseId = warehouse.Id;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = product.Id,
                Quantity = 1,
                ActualSellingPrice = 123
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.PageName.ShouldBe("/Sales/Details");
        model.ModelState.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Save_Multiple_Valid_Lines()
    {
        var context = await CreateSalesContextAsync("SALES-MLP");
        var secondComponent = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SML-C"), Name = "Second Sales Component", Unit = "Piece" });
        await PostComponentReceiptAsync(context.WarehouseId, secondComponent.Id, 10);
        var secondProduct = await CreateProductAsync("SML-P", "Second Sales Product");
        await PublishBomAsync(secondProduct.Id, secondComponent.Id);
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = context.CustomerId;
        model.Input.WarehouseId = context.WarehouseId;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = context.ProductId,
                Quantity = 2,
                ActualSellingPrice = 100
            },
            new CreateSalesOrderLineDto
            {
                ProductId = secondProduct.Id,
                Quantity = 3,
                ActualSellingPrice = 75
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.PageName.ShouldBe("/Sales/Details");
        var orderId = redirect.RouteValues!["id"].ShouldBeOfType<Guid>();
        var order = await GetRequiredService<ISalesOrderAppService>().GetAsync(orderId);
        order.Lines.Count.ShouldBe(2);
        order.Lines.Select(x => x.ProductId).ShouldBe([context.ProductId, secondProduct.Id], ignoreOrder: true);
        order.Lines.Sum(x => x.Quantity).ShouldBe(5);
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Bind_Override_Error_To_Correct_Line_Index()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-ORI");
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = context.CustomerId;
        model.Input.WarehouseId = context.WarehouseId;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = context.ProductId,
                Quantity = 1,
                ActualSellingPrice = 100
            },
            new CreateSalesOrderLineDto
            {
                ProductId = context.ProductId,
                Quantity = 1,
                ActualSellingPrice = 90
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState.ContainsKey("Input.Lines[0].OverrideReason").ShouldBeFalse();
        model.ModelState[$"Input.Lines[1].OverrideReason"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
    }

    [Fact]
    public async Task Sales_Create_Should_Expose_Preloaded_Product_Context_Map()
    {
        var product = await CreateProductAsync("SALES-MAP", "Sales Map Product");
        await PublishBomAsync(product.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Sales/Create"));

        html.ShouldContain("sales-product-context-data");
        html.ShouldContain(product.Id.ToString());
        html.ShouldContain("\"hasPublishedBom\":true");
    }

    [Fact]
    public async Task Sales_Create_Initial_And_Dynamic_Rows_Should_Share_Product_Context_Init_Path()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.cshtml"));
        var linesScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesCreateLines.js"));
        var contextScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));

        CountOccurrences(pageSource, "data-sales-line-row").ShouldBeGreaterThanOrEqualTo(2);
        CountOccurrences(pageSource, "data-sales-product-select").ShouldBeGreaterThanOrEqualTo(2);
        CountOccurrences(pageSource, "data-sales-product-context").ShouldBeGreaterThanOrEqualTo(2);
        CountOccurrences(pageSource, "data-sales-product-eligibility").ShouldBeGreaterThanOrEqualTo(2);
        pageSource.ShouldContain("name=\"Input.Lines[@(i)].Quantity\"");
        pageSource.ShouldContain("name=\"Input.Lines[@(i)].ActualSellingPrice\"");
        pageSource.ShouldContain("asp-for=\"Input.Lines[i].ProductId\"");
        pageSource.ShouldContain("asp-for=\"Input.Lines[i].OverrideReason\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ProductId\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].Quantity\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ActualSellingPrice\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].OverrideReason\"");

        linesScriptSource.ShouldContain("function prepareLineRow(row)");
        linesScriptSource.ShouldContain("getLiveRows(container).forEach(prepareLineRow)");
        linesScriptSource.ShouldContain("bootExistingRows(container);");
        linesScriptSource.ShouldContain("productContext.initializeRow(row)");
        linesScriptSource.ShouldContain("prepareLineRow(row);");

        contextScriptSource.ShouldContain("function initializeRow(row)");
        contextScriptSource.ShouldContain("bindRow(row);");
        contextScriptSource.ShouldContain("loadProductContext(scope);");
        contextScriptSource.ShouldContain("loadProductContextFromMap(scope, productSelector.value, options);");
        contextScriptSource.ShouldContain("actualPriceInput && !actualPriceInput.value");
        contextScriptSource.ShouldContain("scope.querySelector('[data-sales-product-context]')");
        contextScriptSource.ShouldNotContain("scope.dataset.salesContextBound === 'true'");
    }

    [Fact]
    public void Sales_Create_PageModel_Should_Validate_Line_Eligibility_Before_Create()
    {
        var pageSource = File.ReadAllText(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.cshtml.cs"));
        pageSource.ShouldContain("ValidateLineEligibility");
        pageSource.ShouldContain("ValidateLineStockAvailabilityAsync");
        pageSource.ShouldContain("OnGetStockAvailabilityAsync");
        pageSource.ShouldContain("CalculateStockAvailabilityAsync");
        pageSource.ShouldContain("GetPublishedMapByProductIdsAsync");
        pageSource.ShouldContain("IInventoryBalanceRepository");
        pageSource.ShouldContain("SalesBomMustBePublished");
        pageSource.ShouldContain("Sales:ProductStockSaleNotSupported");
        pageSource.ShouldContain("Sales:StockIssueGlobal");
        pageSource.ShouldContain("GetProductContextsJson");
    }

    [Fact]
    public async Task Sales_Create_ProductContext_Handler_Should_Return_Published_Bom_Status()
    {
        var product = await CreateProductAsync("SALES-CTX", "Sales Context Product");
        await PublishBomAsync(product.Id);
        var model = GetRequiredService<CreateModel>();

        var result = await model.OnGetProductContextAsync(product.Id);
        var json = result.Value as SalesProductContextViewModel;

        json.ShouldNotBeNull();
        json!.ProductLabel.ShouldContain(product.Code);
        json.HasPublishedBom.ShouldBeTrue();
        json.BomStatusText.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Handler_Should_Return_Bom_Component_Available_To_Sell()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SA-W1"), Name = "Availability Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SA-C1"), Name = "Availability Component", Unit = "Piece" });
        await PostComponentReceiptAsync(warehouse.Id, component.Id, 5);
        var product = await CreateProductAsync("SA-P1", "Availability Product");
        await PublishBomAsync(product.Id, component.Id, 2);
        var model = GetRequiredService<CreateModel>();

        var response = await GetStockAvailabilityAsync(model, warehouse.Id,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = product.Id, Quantity = 2 });

        var line = response.Lines.Single();
        line.Status.ShouldBe(SalesStockAvailabilityStatus.Available);
        line.AvailableToSell.ShouldBe(2);
        line.IsShortage.ShouldBeFalse();
        line.LimitingComponentLabel.ShouldContain(component.Code);
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Handler_Should_Use_Limiting_Component()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SA-W2"), Name = "Limiting Warehouse" });
        var firstComponent = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SA-C2A"), Name = "Abundant Component", Unit = "Piece" });
        var limitingComponent = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SA-C2B"), Name = "Limiting Component", Unit = "Piece" });
        await PostComponentReceiptAsync(warehouse.Id, firstComponent.Id, 10);
        await PostComponentReceiptAsync(warehouse.Id, limitingComponent.Id, 3);
        var product = await CreateProductAsync("SA-P2", "Limited Product");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items =
            [
                new CreateBomItemDto { ComponentId = firstComponent.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = limitingComponent.Id, Quantity = 1 }
            ]
        });
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);
        var model = GetRequiredService<CreateModel>();

        var response = await GetStockAvailabilityAsync(model, warehouse.Id,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = product.Id, Quantity = 3 });

        var line = response.Lines.Single();
        line.Status.ShouldBe(SalesStockAvailabilityStatus.Available);
        line.AvailableToSell.ShouldBe(3);
        line.LimitingComponentId.ShouldBe(limitingComponent.Id);
        line.LimitingComponentLabel.ShouldContain(limitingComponent.Code);
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Handler_Should_Return_Zero_When_Component_Has_No_Balance()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SA-W3"), Name = "No Balance Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SA-C3"), Name = "No Balance Component", Unit = "Piece" });
        var product = await CreateProductAsync("SA-P3", "No Balance Product");
        await PublishBomAsync(product.Id, component.Id);
        var model = GetRequiredService<CreateModel>();

        var response = await GetStockAvailabilityAsync(model, warehouse.Id,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = product.Id, Quantity = 1 });

        var line = response.Lines.Single();
        line.Status.ShouldBe(SalesStockAvailabilityStatus.Shortage);
        line.AvailableToSell.ShouldBe(0);
        line.IsShortage.ShouldBeTrue();
        line.LimitingComponentLabel.ShouldContain(component.Code);
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Handler_Should_Detect_Aggregate_Multi_Line_Component_Shortage()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SA-W4"), Name = "Aggregate Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SA-C4"), Name = "Shared Component", Unit = "Piece" });
        await PostComponentReceiptAsync(warehouse.Id, component.Id, 5);
        var firstProduct = await CreateProductAsync("SA-P4A", "First Aggregate Product");
        var secondProduct = await CreateProductAsync("SA-P4B", "Second Aggregate Product");
        await PublishBomAsync(firstProduct.Id, component.Id, 2);
        await PublishBomAsync(secondProduct.Id, component.Id, 3);
        var model = GetRequiredService<CreateModel>();

        var response = await GetStockAvailabilityAsync(
            model,
            warehouse.Id,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = firstProduct.Id, Quantity = 2 },
            new SalesStockAvailabilityLineRequest { LineIndex = 1, ProductId = secondProduct.Id, Quantity = 1 });

        response.Lines.Count.ShouldBe(2);
        response.Lines.ShouldAllBe(x => x.Status == SalesStockAvailabilityStatus.Shortage);
        response.Lines.ShouldAllBe(x => x.IsShortage);
        response.Lines.ShouldAllBe(x => x.LimitingComponentId == component.Id);
        response.Lines.Select(x => x.LimitingComponentRequiredQuantity).Distinct().Single().ShouldBe(7);
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Handler_Should_Return_NoBom_For_Product_Without_Published_Bom()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SA-W5"), Name = "No BOM Warehouse" });
        var product = await CreateProductAsync("SA-P5", "No BOM Availability Product");
        var model = GetRequiredService<CreateModel>();

        var response = await GetStockAvailabilityAsync(model, warehouse.Id,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = product.Id, Quantity = 1 });

        var line = response.Lines.Single();
        line.Status.ShouldBe(SalesStockAvailabilityStatus.NoBom);
        line.IsShortage.ShouldBeFalse();
        line.AvailableToSell.ShouldBe(0);
        line.LimitingComponentId.ShouldBeNull();
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Block_When_Component_Stock_Is_Insufficient()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var group = await GetRequiredService<ICustomerGroupAppService>()
            .CreateAsync(new CreateCustomerGroupDto { Code = Unique("SST-G"), Name = "Stock Gate Group" });
        var customer = await GetRequiredService<ICustomerAppService>()
            .CreateAsync(new CreateCustomerDto { Code = Unique("SST-C"), Name = "Stock Gate Customer", CustomerGroupId = group.Id });
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SST-W"), Name = "Stock Gate Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SST-I"), Name = "Stock Gate Component", Unit = "Piece" });
        await PostComponentReceiptAsync(warehouse.Id, component.Id, 2);
        var product = await CreateProductAsync("SST-P", "Stock Gate Product");
        await PublishBomAsync(product.Id, component.Id);
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = customer.Id;
        model.Input.WarehouseId = warehouse.Id;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = product.Id,
                Quantity = 3,
                ActualSellingPrice = 123
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"Input.Lines[0].Quantity"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(x => x.Contains(localizer["Sales:InsufficientStockForRequestedQuantity"].Value));
        model.ModelState[string.Empty]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer["Sales:StockIssueGlobal"].Value);
    }

    [Fact]
    public async Task Sales_Create_OnPostAsync_Should_Block_Aggregate_Multi_Line_Component_Shortage()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var group = await GetRequiredService<ICustomerGroupAppService>()
            .CreateAsync(new CreateCustomerGroupDto { Code = Unique("SAG-G"), Name = "Aggregate Save Group" });
        var customer = await GetRequiredService<ICustomerAppService>()
            .CreateAsync(new CreateCustomerDto { Code = Unique("SAG-C"), Name = "Aggregate Save Customer", CustomerGroupId = group.Id });
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique("SAG-W"), Name = "Aggregate Save Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("SAG-I"), Name = "Aggregate Save Component", Unit = "Piece" });
        await PostComponentReceiptAsync(warehouse.Id, component.Id, 5);
        var firstProduct = await CreateProductAsync("SAG-P1", "Aggregate Save First Product");
        var secondProduct = await CreateProductAsync("SAG-P2", "Aggregate Save Second Product");
        await PublishBomAsync(firstProduct.Id, component.Id, 2);
        await PublishBomAsync(secondProduct.Id, component.Id, 3);
        var model = GetRequiredService<CreateModel>();
        await model.OnGetAsync();
        model.Input.CustomerId = customer.Id;
        model.Input.WarehouseId = warehouse.Id;
        model.Input.OrderDate = DateTime.UtcNow.Date;
        model.Input.Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId = firstProduct.Id,
                Quantity = 2,
                ActualSellingPrice = 100
            },
            new CreateSalesOrderLineDto
            {
                ProductId = secondProduct.Id,
                Quantity = 1,
                ActualSellingPrice = 75
            }
        ];

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"Input.Lines[0].Quantity"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(x => x.Contains(localizer["Sales:InsufficientStockForRequestedQuantity"].Value));
        model.ModelState[$"Input.Lines[1].Quantity"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(x => x.Contains(localizer["Sales:InsufficientStockForRequestedQuantity"].Value));
        model.ModelState[string.Empty]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer["Sales:StockIssueGlobal"].Value);
    }

    [Fact]
    public async Task Sales_Create_StockAvailability_Client_Should_Refresh_On_Page_And_Row_Changes()
    {
        var contextScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));
        var linesScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesCreateLines.js"));

        contextScriptSource.ShouldContain("document.addEventListener('DOMContentLoaded'");
        contextScriptSource.ShouldContain("bindWarehouseSelector();");
        contextScriptSource.ShouldContain("warehouseSelector.addEventListener('change'");
        contextScriptSource.ShouldContain("productSelector.addEventListener('change', onProductChanged)");
        contextScriptSource.ShouldContain("quantityInput.addEventListener('input'");
        contextScriptSource.ShouldContain("refreshStockAvailability(getLinesContainer())");
        contextScriptSource.ShouldContain("row.dataset.salesStockStatus === 'shortage'");
        contextScriptSource.ShouldContain("setStockAvailability(row, 'noBom', getNoBomStockAvailabilityMessage())");
        contextScriptSource.ShouldContain("buildAvailabilityUrl(warehouseId, lines)");
        contextScriptSource.ShouldContain("getValue(data, 'Lines') || []");

        linesScriptSource.ShouldContain("bootExistingRows(container);");
        linesScriptSource.ShouldContain("linesBody.appendChild(row)");
        linesScriptSource.ShouldContain("row.remove();");
        CountOccurrences(linesScriptSource, "productContext.refreshStockAvailability").ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Sales_Stock_Display_Text_Should_Use_Current_Vietnamese_Component_Terminology()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var stockMessages = new[]
        {
            localizer["Sales:ProductStockSaleNotSupported"].Value,
            localizer["Sales:AvailableToSellAtWarehouse", "1"].Value,
            localizer["Sales:InsufficientStockForRequestedQuantity"].Value,
            localizer["Sales:MissingComponentStock", "Vật tư A"].Value,
            localizer["Sales:NoBomStockAvailabilityUnavailable"].Value,
            localizer["Sales:StockIssueGlobal"].Value
        };
        var legacyComponentTerm = "Linh" + " kiện";

        stockMessages.ShouldAllBe(x => !x.Contains(legacyComponentTerm, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SalesUiFormatter_Should_Return_Localized_Business_Error()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var message = SalesUiFormatter.GetFriendlyErrorMessage(
            localizer,
            new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished));

        message.ShouldBe(localizer[VPureLuxDomainErrorCodes.SalesBomMustBePublished].Value);
    }

    [Fact]
    public async Task Sales_Edit_Should_Render_Product_Label_And_Bom_Badge()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-ED");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Edit/{order.Id}"));

        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain("badge");
        html.ShouldContain(localizer["Sales:PublishedBomAvailable"].Value);
        html.ShouldContain("data-sales-product-context");
    }

    [Fact]
    public async Task Sales_Edit_Should_Render_Multiple_Existing_Lines_And_Baseline_Line_Handlers()
    {
        var context = await CreateSalesContextAsync("SALES-EML");
        var secondProduct = await CreateProductAsync("SALES-EML-P2", "Sales Edit Second Product");
        await PublishBomAsync(secondProduct.Id);
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines =
            [
                new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 },
                new CreateSalesOrderLineDto { ProductId = secondProduct.Id, Quantity = 2, ActualSellingPrice = 75 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Edit/{order.Id}"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Edit.cshtml"));
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Edit.cshtml.cs"));

        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain($"{secondProduct.Code} - {secondProduct.Name}");
        html.ShouldContain("sales-edit-lines-editor");
        html.ShouldContain("sales-edit-lines-table");
        html.ShouldContain("data-sales-line-row");
        html.ShouldContain("data-sales-stock-availability");
        html.ShouldContain("data-sales-actual-price");
        html.ShouldContain("data-sales-override-validation");
        html.ShouldContain("data-sales-availability-endpoint");
        html.ShouldContain("data-sales-warehouse-id");
        html.ShouldContain("name=\"LineId\"");
        html.ShouldContain("name=\"lineId\"");
        html.ShouldContain("name=\"UpdateLine.Quantity\"");
        html.ShouldContain("name=\"UpdateLine.ActualSellingPrice\"");
        html.ShouldContain("name=\"UpdateLine.OverrideReason\"");
        html.ShouldContain("name=\"NewLine.ProductId\"");
        html.ShouldContain("name=\"NewLine.Quantity\"");
        html.ShouldContain("name=\"NewLine.ActualSellingPrice\"");
        html.ShouldContain("name=\"NewLine.OverrideReason\"");
        pageSource.ShouldContain("asp-page-handler=\"Add\"");
        pageSource.ShouldContain("asp-page-handler=\"Remove\"");
        pageSource.ShouldContain("asp-page-handler=\"Update\"");
        pageSource.ShouldContain("form=\"@updateFormId\"");
        pageSource.ShouldContain("form=\"@addFormId\"");
        pageSource.ShouldContain("data-sales-product-select");
        pageSource.ShouldContain("data-sales-product-context");
        pageSource.ShouldContain("sales-product-context-data");
        pageSource.ShouldContain("GetProductContextsJson");
        pageSource.ShouldContain("<abp-style src=\"/Pages/Sales/Create.css\" />");
        pageModelSource.ShouldContain("OnPostAddAsync");
        pageModelSource.ShouldContain("OnPostRemoveAsync");
        pageModelSource.ShouldContain("OnPostUpdateAsync");
        pageModelSource.ShouldContain("OnGetStockAvailabilityAsync");
        pageModelSource.ShouldContain("ValidateNewLineStockAvailabilityAsync");
        pageModelSource.ShouldContain("ValidateUpdateLineStockAvailabilityAsync");
    }

    [Fact]
    public async Task Sales_Edit_Should_ReUse_Product_Context_And_Stock_Availability_Client_Hooks()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Edit.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/SalesProductContext.js"));
        var createCss = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.css"));

        pageSource.ShouldContain("id=\"SalesEditPage\"");
        pageSource.ShouldContain("data-sales-context-endpoint");
        pageSource.ShouldContain("data-sales-availability-endpoint");
        pageSource.ShouldContain("data-sales-product-not-eligible");
        pageSource.ShouldContain("data-sales-override-reason-required");
        pageSource.ShouldContain("data-sales-stock-availability");
        pageSource.ShouldContain("data-sales-product-eligibility");
        pageSource.ShouldContain("data-sales-edit-alert");

        scriptSource.ShouldContain("var editPage = document.getElementById('SalesEditPage')");
        scriptSource.ShouldContain("var availabilityPage = createPage || editPage");
        scriptSource.ShouldContain("document.getElementById('sales-create-lines') || document.getElementById('sales-edit-lines')");
        scriptSource.ShouldContain("availabilityPage.dataset.salesWarehouseId");
        scriptSource.ShouldContain("availabilityPage.dataset.salesAvailabilityEndpoint");
        scriptSource.ShouldContain("refreshStockAvailability(getLinesContainer());");
        scriptSource.ShouldContain("setStockAvailability(scope, 'noBom', getNoBomStockAvailabilityMessage())");
        createCss.ShouldContain(".sales-edit-lines-table");
        createCss.ShouldContain(".sales-edit-add-line-row");
    }

    [Fact]
    public async Task Sales_Edit_AddLine_Should_Block_NoBom_Product_With_Field_Error()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-ENB");
        var ineligibleProduct = await CreateProductAsync("SALES-ENB-P", "Edit No BOM Product");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        var model = GetRequiredService<EditModel>();
        SetPageContext(model);
        model.Id = order.Id;
        model.NewLine = new CreateSalesOrderLineDto
        {
            ProductId = ineligibleProduct.Id,
            Quantity = 1,
            ActualSellingPrice = 100
        };

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAddAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"NewLine.ProductId"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer["Sales:ProductStockSaleNotSupported"].Value);
        localizer["Sales:ProductStockSaleNotSupported"].Value.ShouldContain("tồn kho thành phẩm");
    }

    [Fact]
    public async Task Sales_Edit_AddLine_Should_Save_Missing_Suggested_Price_With_Manual_Actual_Price()
    {
        var context = await CreateSalesContextAsync("SALES-ENS");
        var secondComponent = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique("ENS-C"), Name = "Edit No Suggested Component", Unit = "Piece" });
        await PostComponentReceiptAsync(context.WarehouseId, secondComponent.Id, 5);
        var noSuggestedProduct = await CreateProductAsync("ENS-P", "Edit No Suggested Product");
        await PublishBomAsync(noSuggestedProduct.Id, secondComponent.Id);
        var service = GetRequiredService<ISalesOrderAppService>();
        var order = await service.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        var model = GetRequiredService<EditModel>();
        SetPageContext(model);
        model.Id = order.Id;
        model.NewLine = new CreateSalesOrderLineDto
        {
            ProductId = noSuggestedProduct.Id,
            Quantity = 1,
            ActualSellingPrice = 123
        };

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostAddAsync());

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.RouteValues!["id"].ShouldBe(order.Id);
        var updated = await service.GetAsync(order.Id);
        updated.Lines.Count.ShouldBe(2);
        updated.Lines.ShouldContain(x => x.ProductId == noSuggestedProduct.Id && x.ActualSellingPrice == 123);
    }

    [Fact]
    public async Task Sales_Edit_UpdateLine_Should_Require_Override_Reason_When_Suggested_Price_Differs()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-EOR");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        var line = order.Lines.Single();
        var model = GetRequiredService<EditModel>();
        SetPageContext(model);
        model.Id = order.Id;
        model.LineId = line.Id;
        model.UpdateLine = new UpdateSalesOrderLineDto
        {
            Quantity = 1,
            ActualSellingPrice = 90
        };

        var result = await WithSalesUnitOfWorkAsync(() => model.OnPostUpdateAsync());

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"UpdateLine.OverrideReason"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
    }

    [Fact]
    public async Task Sales_Edit_StockAvailability_Handler_Should_Return_Bom_Component_Available_To_Sell()
    {
        var context = await CreateSalesContextAsync("SALES-ESA");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        var model = GetRequiredService<EditModel>();
        model.Id = order.Id;

        var response = await GetEditStockAvailabilityAsync(model,
            new SalesStockAvailabilityLineRequest { LineIndex = 0, ProductId = context.ProductId, Quantity = 2 });

        var line = response.Lines.Single();
        line.Status.ShouldBe(SalesStockAvailabilityStatus.Available);
        line.AvailableToSell.ShouldBeGreaterThanOrEqualTo(2);
        line.IsShortage.ShouldBeFalse();
    }

    [Fact]
    public async Task Sales_Details_Should_Render_Product_Label_Bom_And_Profit_Without_Raw_Ids()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-DT");
        var salesService = GetRequiredService<ISalesOrderAppService>();
        var order = await salesService.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        await salesService.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));

        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain(localizer["Sales:PublishedBomVersion", 1].Value);
        html.ShouldContain(localizer["Sales:Profit"].Value);

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Details.cshtml"));
        pageSource.ShouldContain("data-sales-action-form");
        pageSource.ShouldContain("VPureLuxPermissions.Sales.ViewCost");
        pageSource.ShouldContain("VPureLuxPermissions.Sales.ViewProfit");
        pageSource.ShouldNotContain("LineType");
        pageSource.ShouldNotContain("ComponentId");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
    }

    [Fact]
    public async Task Sales_Index_And_Details_Should_Format_Values_For_Vietnamese_Display()
    {
        var context = await CreateSalesContextAsync("SALES-FMT");
        var salesService = GetRequiredService<ISalesOrderAppService>();
        var orderDate = new DateTime(2026, 6, 18);
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        const decimal quantity = 2.25m;
        const decimal unitPrice = 310000m;
        var expectedRevenue = decimal.Round(quantity * unitPrice, 0, MidpointRounding.AwayFromZero).ToString("#,0", vi) + " ₫";
        var expectedPrice = unitPrice.ToString("#,0", vi) + " ₫";
        var expectedQuantity = quantity.ToString("0.####", vi);

        var order = await salesService.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            OrderDate = orderDate,
            Lines =
            [
                new CreateSalesOrderLineDto
                {
                    ProductId = context.ProductId,
                    Quantity = quantity,
                    ActualSellingPrice = unitPrice
                }
            ]
        });
        await salesService.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var indexHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Sales"));
        indexHtml.ShouldContain(orderDate.ToString("dd/MM/yyyy", vi));
        indexHtml.ShouldContain(expectedRevenue);
        indexHtml.ShouldNotContain($"{(quantity * unitPrice):0.000000}");

        var detailsHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));
        detailsHtml.ShouldContain(expectedRevenue);
        detailsHtml.ShouldContain(expectedPrice);
        detailsHtml.ShouldContain(expectedQuantity);
        detailsHtml.ShouldNotContain("310000.000000");
        detailsHtml.ShouldNotContain("2.2500");

        var indexSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Index.cshtml"));
        indexSource.ShouldContain("FormatDate(order.OrderDate)");
        indexSource.ShouldContain("FormatMoney(order.TotalRevenueAmount)");

        var detailsSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Details.cshtml"));
        detailsSource.ShouldContain("canViewCost");
        detailsSource.ShouldContain("canViewProfit");
        detailsSource.ShouldContain("FormatMoney(Model.Order.TotalCostAmount)");
        detailsSource.ShouldContain("FormatMoney(line.ProfitAmount)");
    }

    [Fact]
    public async Task Sales_Details_Should_Guard_Cost_Columns_With_ViewCost()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Details.cshtml"));

        pageSource.ShouldContain("canViewCost");
        pageSource.ShouldContain("VPureLuxPermissions.Sales.ViewCost");
        pageSource.ShouldContain("Model.Order.TotalCostAmount");
        pageSource.ShouldContain("line.CostPriceSnapshot");
        pageSource.ShouldContain("line.CostAmountSnapshot");
    }

    [Fact]
    public async Task Sales_CustomerHistory_Should_Render_Product_Labels_And_Summary_Cards()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-CH");
        var salesService = GetRequiredService<ISalesOrderAppService>();
        var order = await salesService.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        await salesService.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/CustomerHistory?CustomerId={context.CustomerId}"));

        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain(localizer["Sales:SummaryProductCount"].Value);
        html.ShouldContain(localizer["Sales:CustomerHistoryFor", $"{context.CustomerCode} - {context.CustomerName}"].Value);

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/CustomerHistory.cshtml"));
        pageSource.ShouldNotContain("@item.ProductId");
        pageSource.ShouldNotContain("@item.CustomerId");
    }

    [Fact]
    public async Task Sales_CustomerHistory_Should_Render_Empty_State()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var group = await GetRequiredService<ICustomerGroupAppService>()
            .CreateAsync(new CreateCustomerGroupDto { Code = Unique("SG"), Name = "Sales Empty Group" });
        var customer = await GetRequiredService<ICustomerAppService>()
            .CreateAsync(new CreateCustomerDto { Code = Unique("SC"), Name = "Sales Empty Customer", CustomerGroupId = group.Id });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/CustomerHistory?CustomerId={customer.Id}"));

        html.ShouldContain(localizer["Sales:NoPurchaseHistory"].Value);
        html.ShouldNotContain(localizer["Sales:SummaryProductCount"].Value);
    }

    [Fact]
    public void SalesUiFormatter_Should_Fallback_To_Exception_Message_For_Unknown_Code()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var message = SalesUiFormatter.GetFriendlyErrorMessage(
            localizer,
            new BusinessException("UNKNOWN_CODE", "Specific operator message."));

        message.ShouldBe("Specific operator message.");
    }

    [Fact]
    public async Task Sales_Pages_Should_Not_Use_Direct_Component_Sales_Selectors()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Sales/Create.cshtml",
            "src/VPureLux.Web/Pages/Sales/Edit.cshtml",
            "src/VPureLux.Web/Pages/Sales/Details.cshtml",
            "src/VPureLux.Web/Pages/Sales/CustomerHistory.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldNotContain("LineType");
            pageSource.ShouldNotContain("CatalogItemId");
            pageSource.ShouldNotContain("ComponentId");
        }
    }

    [Fact]
    public async Task Sales_Product_Context_PageModels_Should_Use_Scoped_Product_Pricing_Context()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Sales/Create.cshtml.cs",
            "src/VPureLux.Web/Pages/Sales/Edit.cshtml.cs",
            "src/VPureLux.Web/Pages/Sales/Details.cshtml.cs"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldContain("FindMapAsync");
            pageSource.ShouldNotContain("_productPricingContext.GetListAsync()");
        }
    }

    [Fact]
    public async Task Sales_Details_Should_Display_Customer_For_Draft_Order()
    {
        var context = await CreateSalesContextAsync("SALES-DDC");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));

        html.ShouldContain($"{context.CustomerCode} - {context.CustomerName}");
        html.ShouldNotContain("> - <");
    }

    [Fact]
    public async Task Sales_Details_Draft_Should_Show_Estimated_Revenue_And_Confirmed_Total_Labels()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-DTR");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 2, ActualSellingPrice = 100 }]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));

        html.ShouldContain(localizer["Sales:DraftEstimatedRevenue"].Value);
        html.ShouldContain(localizer["Sales:ConfirmedRevenue"].Value);
        html.ShouldContain(localizer["Sales:ConfirmedCost"].Value);
        html.ShouldContain(localizer["Sales:ConfirmedProfit"].Value);
        html.ShouldContain(localizer["Sales:DraftFinancialTotalsNote"].Value);
        html.ShouldContain(localizer["Sales:CalculatedAfterConfirmation"].Value);
        CountOccurrences(html, localizer["Sales:CalculatedAfterConfirmation"].Value).ShouldBeGreaterThanOrEqualTo(3);
        html.ShouldContain(FormatMoneyForTest(200));
        html.ShouldContain(FormatMoneyForTest(0));
    }

    [Fact]
    public async Task Sales_Details_Draft_Should_Sum_Estimated_Revenue_For_Multiple_Lines()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-DTRM");
        var secondProduct = await CreateProductAsync("SALES-DTRM-P2", "Sales Draft Revenue Second Product");
        await PublishBomAsync(secondProduct.Id);
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines =
            [
                new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 2, ActualSellingPrice = 100 },
                new CreateSalesOrderLineDto { ProductId = secondProduct.Id, Quantity = 3, ActualSellingPrice = 75 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));

        html.ShouldContain(localizer["Sales:DraftEstimatedRevenue"].Value);
        html.ShouldContain(FormatMoneyForTest(425));
        html.ShouldContain(localizer["Sales:CalculatedAfterConfirmation"].Value);
        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain($"{secondProduct.Code} - {secondProduct.Name}");
    }

    [Fact]
    public async Task Sales_Details_Confirm_Button_Should_Post_To_Confirm_Handler()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Details.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Details.js"));

        pageSource.ShouldContain("asp-page-handler=\"Confirm\"");
        pageSource.ShouldContain("data-sales-action-form");
        pageSource.ShouldContain("asp-for=\"Confirmation.IdempotencyKey\"");
        pageSource.ShouldContain("data-confirm-message");
        scriptSource.ShouldContain("form.submit();");
        scriptSource.ShouldContain("form.dataset.confirmMessage");
    }

    [Fact]
    public void Sales_Details_Confirm_Handler_Should_Disable_Page_UnitOfWork()
    {
        var attribute = typeof(DetailsModel)
            .GetMethod(nameof(DetailsModel.OnPostConfirmAsync))!
            .GetCustomAttribute<UnitOfWorkAttribute>();

        attribute.ShouldNotBeNull();
        attribute.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Sales_Details_OnPostConfirmAsync_Should_Redirect_And_Update_Status_When_Confirm_Succeeds()
    {
        var context = await CreateSalesContextAsync("SALES-COK");
        var service = GetRequiredService<ISalesOrderAppService>();
        var order = await service.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
        });
        var model = GetRequiredService<DetailsModel>();
        SetPageContext(model);
        model.Id = order.Id;
        model.Confirmation = new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") };

        var result = await model.OnPostConfirmAsync();

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.RouteValues!["id"].ShouldBe(order.Id);
        model.SuccessMessage.ShouldBe(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Sales:ConfirmedSuccessfully"].Value);
        (await service.GetAsync(order.Id)).Status.ShouldBe(SalesOrderStatus.Confirmed);
    }

    [Fact]
    public async Task Sales_Details_OnPostConfirmAsync_Should_Show_Friendly_Error_When_Inventory_Blocks_Confirm()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateSalesContextAsync("SALES-CERR");
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 25, ActualSellingPrice = 100 }]
        });
        var model = GetRequiredService<DetailsModel>();
        SetPageContext(model);
        model.Id = order.Id;
        model.Confirmation = new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") };

        var result = await model.OnPostConfirmAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.Order.Status.ShouldBe(SalesOrderStatus.Draft);
        model.ConfirmErrorMessage.ShouldNotBeNullOrWhiteSpace();
        model.ConfirmErrorMessage.ShouldContain(localizer[VPureLuxDomainErrorCodes.SalesInventoryValidationFailed].Value);
        model.ConfirmErrorMessage.ShouldContain(localizer[VPureLuxDomainErrorCodes.InsufficientInventory].Value);
    }

    [Fact]
    public async Task Sales_Details_OnPostConfirmAsync_Should_Show_Friendly_Error_When_Db_Concurrency_Blocks_Confirm()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var orderId = Guid.NewGuid();
        var service = Substitute.For<ISalesOrderAppService>();
        service.ConfirmAsync(orderId, Arg.Any<ConfirmSalesOrderDto>())
            .Returns<Task<ConfirmSalesOrderResultDto>>(_ => throw new AbpDbConcurrencyException("Expected concurrency test exception."));
        service.GetAsync(orderId).Returns(new SalesOrderDto
        {
            Id = orderId,
            OrderNo = "SO-CONCURRENCY",
            CustomerId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Status = SalesOrderStatus.Draft,
            CustomerCodeSnapshot = "C-CON",
            CustomerNameSnapshot = "Concurrency Customer"
        });
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
        var customers = Substitute.For<ICustomerAppService>();
        var products = Substitute.For<IProductAppService>();
        products.GetListAsync(Arg.Any<GetProductListInput>())
            .Returns(new PagedResultDto<ProductDto>());
        var pricingContext = Substitute.For<IProductPricingContextLookupService>();
        pricingContext.FindMapAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTime>())
            .Returns(new Dictionary<Guid, ProductPricingContextDto>());
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        var model = new DetailsModel(service, authorization, customers, products, pricingContext, unitOfWorkManager);
        SetPageContext(model, GetRequiredService<IServiceProvider>());
        model.ProductContexts[Guid.NewGuid()] = new SalesProductContextViewModel();
        model.Id = orderId;
        model.Confirmation = new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") };

        var result = await model.OnPostConfirmAsync();

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.RouteValues!["id"].ShouldBe(orderId);
        model.ModelState.IsValid.ShouldBeTrue();
        model.ConfirmErrorMessage.ShouldBe(localizer["Sales:ConfirmConcurrencyError"].Value);
    }

    [Fact]
    public async Task Sales_Details_Should_Render_All_Multi_Line_Order_Lines()
    {
        var context = await CreateSalesContextAsync("SALES-DML");
        var secondProduct = await CreateProductAsync("SALES-DML-P2", "Sales Details Second Product");
        await PublishBomAsync(secondProduct.Id);
        var order = await GetRequiredService<ISalesOrderAppService>().CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines =
            [
                new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 },
                new CreateSalesOrderLineDto { ProductId = secondProduct.Id, Quantity = 2, ActualSellingPrice = 75 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Sales/Details/{order.Id}"));

        html.ShouldContain($"{context.ProductCode} - {context.ProductName}");
        html.ShouldContain($"{secondProduct.Code} - {secondProduct.Name}");
        html.ShouldContain(FormatMoneyForTest(100));
        html.ShouldContain(FormatMoneyForTest(75));
        order.Lines.Count.ShouldBe(2);
    }

    private async Task<(Guid CustomerId, string CustomerCode, string CustomerName, Guid WarehouseId, Guid ProductId, string ProductCode, string ProductName)> CreateSalesContextAsync(string prefix)
    {
        var groups = GetRequiredService<ICustomerGroupAppService>();
        var customers = GetRequiredService<ICustomerAppService>();
        var warehouses = GetRequiredService<IWarehouseAppService>();
        var components = GetRequiredService<IComponentAppService>();
        var products = GetRequiredService<IProductAppService>();
        var stockItems = GetRequiredService<IStockItemRepository>();
        var inventory = GetRequiredService<IInventoryTransactionAppService>();
        var group = await groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique($"{prefix}-G"), Name = "Sales Page Group" });
        var customer = await customers.CreateAsync(new CreateCustomerDto { Code = Unique($"{prefix}-C"), Name = "Sales Page Customer", CustomerGroupId = group.Id });
        var warehouse = await warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique($"{prefix}-W"), Name = "Sales Page Warehouse" });
        var component = await components.CreateAsync(new CreateComponentDto { Code = Unique($"{prefix}-I"), Name = "Sales Page Component", Unit = "Piece" });
        var stockItem = (await stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new ReceiptLineInput { StockItemId = stockItem.Id, Quantity = 20, UnitCost = 50, LotNo = Unique($"{prefix}-L"), ReceivedAt = DateTime.UtcNow }]
        });
        var product = await products.CreateAsync(new CreateProductDto { Code = Unique($"{prefix}-P"), Name = "Sales Page Product" });
        await PublishBomAsync(product.Id, component.Id);
        await GetRequiredService<IProductSuggestedPriceAppService>().CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            Price = 100,
            Reason = "Giá bán đề xuất cho kiểm thử Sales",
            EffectiveFrom = DateTime.Now.Date
        });
        return (customer.Id, customer.Code, customer.Name, warehouse.Id, product.Id, product.Code, product.Name);
    }

    private async Task<ProductDto> CreateProductAsync(string prefix, string name) =>
        await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique(prefix),
            Name = name
        });

    private async Task<SalesStockAvailabilityResponse> GetStockAvailabilityAsync(
        CreateModel model,
        Guid warehouseId,
        params SalesStockAvailabilityLineRequest[] lines)
    {
        return await WithSalesUnitOfWorkAsync(async () =>
        {
            var result = await model.OnGetStockAvailabilityAsync(
                warehouseId,
                JsonSerializer.Serialize(lines, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            return result.Value.ShouldBeOfType<SalesStockAvailabilityResponse>();
        });
    }

    private async Task<SalesStockAvailabilityResponse> GetEditStockAvailabilityAsync(
        EditModel model,
        params SalesStockAvailabilityLineRequest[] lines)
    {
        return await WithSalesUnitOfWorkAsync(async () =>
        {
            var result = await model.OnGetStockAvailabilityAsync(
                JsonSerializer.Serialize(lines, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            return result.Value.ShouldBeOfType<SalesStockAvailabilityResponse>();
        });
    }

    private async Task<TResult> WithSalesUnitOfWorkAsync<TResult>(Func<Task<TResult>> action)
    {
        var unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        using var unitOfWork = unitOfWorkManager.Begin();
        var result = await action();
        await unitOfWork.CompleteAsync();
        return result;
    }

    private async Task PostComponentReceiptAsync(Guid warehouseId, Guid componentId, decimal quantity)
    {
        var stockItem = await GetRequiredService<IStockItemRepository>()
            .FindByCatalogItemAsync(StockItemType.Component, componentId);
        stockItem.ShouldNotBeNull();
        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = stockItem!.Id,
                    Quantity = quantity,
                    UnitCost = 50,
                    LotNo = Unique("SA-L"),
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
    }

    private async Task<Guid> PublishBomAsync(Guid productId, Guid? componentId = null, decimal quantity = 1)
    {
        componentId ??= (await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("SALES-C"),
            Name = "Sales BOM Component",
            Unit = "Piece"
        })).Id;
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(productId, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = componentId.Value, Quantity = quantity }]
        });
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);
        return componentId.Value;
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string FormatMoneyForTest(decimal value)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var amount = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return amount.ToString("#,0", vi) + " ₫";
    }

    private static void SetPageContext(PageModel model, IServiceProvider? services = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        if (services != null)
        {
            httpContext.RequestServices = services;
        }

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };
    }

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "VPureLux.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
