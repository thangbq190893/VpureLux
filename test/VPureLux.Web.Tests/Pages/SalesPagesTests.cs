using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
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
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Xunit;
using CreateModel = VPureLux.Web.Pages.Sales.CreateModel;
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
        html.ShouldContain("data-sales-context-endpoint");
        html.ShouldContain(localizer["Sales:SelectProductForContext"].Value);
        html.ShouldNotContain("value=\"0,00\"");
        html.ShouldNotContain("value='0,00'");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Sales/Create.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Sales/SalesProductContext.js\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Sales/SalesCreateLines.js\" />");
        pageSource.ShouldContain("data-sales-line-row");
        pageSource.ShouldContain("data-sales-product-select");
        pageSource.ShouldContain("id=\"sales-line-row-template\"");
        pageSource.ShouldContain("<template id=\"sales-line-row-template\">");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].ProductId\"");
        pageSource.ShouldContain("id=\"add-sales-line\"");
        pageSource.ShouldContain("remove-sales-line");
        pageSource.ShouldContain("Input.Lines[i].ProductId");
        pageSource.ShouldContain("@for (var i = 0; i < Model.Input.Lines.Count; i++)");
        pageSource.ShouldNotContain("Input.Lines[0].ProductId");
        pageSource.ShouldContain("data-sales-line-index");
        pageSource.ShouldContain("form-select w-100");
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
        scriptSource.ShouldContain("data-sales-product-eligibility");
        scriptSource.ShouldContain("loadProductContextFromMap");

        pageSource.ShouldContain("sales-product-context-data");
        pageSource.ShouldContain("GetProductContextsJson");
        pageSource.ShouldContain("data-sales-product-eligibility");
        pageSource.ShouldContain("data-sales-product-not-eligible");
        pageSource.ShouldContain("data-sales-override-reason-required");
        pageSource.ShouldContain("data-sales-create-alert");
        pageSource.ShouldContain("data-sales-override-validation");
        pageSource.ShouldContain("asp-validation-for=\"Input.Lines[i].ProductId\"");

        linesScriptSource.ShouldContain("validateAllRows");
        linesScriptSource.ShouldContain("data-sales-override-validation");

        scriptSource.ShouldContain("validateOverrideReason");
        scriptSource.ShouldContain("getOverrideReasonRequiredMessage");
        scriptSource.ShouldContain("showCreateAlert");
        scriptSource.ShouldContain("data-sales-create-alert");
        scriptSource.ShouldContain("data-sales-override-validation");
        scriptSource.ShouldContain("Sales:ManualPriceRequired");
        scriptSource.ShouldContain("salesPriceAutoFilled");
        scriptSource.ShouldContain("salesPreviousProductId");
        scriptSource.ShouldContain("SuggestedPrice");
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
        scriptSource.ShouldContain("return l('Sales:NoSuggestedPrice') + ' - ' + getManualPriceRequiredMessage();");
        scriptSource.ShouldContain("if (suggestedPrice === null)");
        localizer["Sales:ManualPriceRequired"].Value.ShouldNotBeNullOrWhiteSpace();
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

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[$"Input.Lines[0].ProductId"]!.Errors
            .Select(x => x.ErrorMessage)
            .ShouldContain(localizer["Sales:ProductNotSaleEligible"].Value);
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

        var result = await model.OnPostAsync();

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

        var result = await model.OnPostAsync();

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
        await PublishBomAsync(product.Id);
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

        var result = await model.OnPostAsync();

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.PageName.ShouldBe("/Sales/Details");
        model.ModelState.IsValid.ShouldBeTrue();
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
        pageSource.ShouldContain("SalesBomMustBePublished");
        pageSource.ShouldContain("Sales:ProductNotSaleEligible");
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

    private async Task PublishBomAsync(Guid productId, Guid? componentId = null)
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
            Items = [new CreateBomItemDto { ComponentId = componentId.Value, Quantity = 1 }]
        });
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);
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
