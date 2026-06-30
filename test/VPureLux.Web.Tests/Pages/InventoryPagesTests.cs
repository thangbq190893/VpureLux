using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using VPureLux.Localization;
using VPureLux.Web.Pages.Inventory;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class InventoryPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Inventory_Query_And_Posting_Pages_Should_Render()
    {
        foreach (var route in new[] { "/Inventory", "/Inventory/Warehouses", "/Inventory/Receipt", "/Inventory/Issue", "/Inventory/Adjustment", "/Inventory/Balances", "/Inventory/Lots", "/Inventory/Ledger" })
        {
            (await GetResponseAsStringAsync(route)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Inventory_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        var model = new Web.Pages.Inventory.IndexModel(authorization)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } }
        };
        await model.OnGetAsync();
        model.CanReceive.ShouldBeFalse();
        model.CanIssue.ShouldBeFalse();
        model.CanAdjust.ShouldBeFalse();
        model.CanManageWarehouses.ShouldBeFalse();
        model.CanViewLedger.ShouldBeFalse();
    }

    [Fact]
    public async Task Receipt_Page_Should_Render_Warehouse_And_StockItem_Selectors()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-R"),
            Name = "Receipt Warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-R"),
            Name = "Receipt Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));

        html.ShouldContain($"{warehouse.Code} - {warehouse.Name}");
        html.ShouldContain($"{component.Code} - {component.Name}");
        html.ShouldContain("name=\"Input.IdempotencyKey\"");
        html.ShouldContain("type=\"hidden\"");
        html.ShouldNotContain("type=\"text\" id=\"Input_IdempotencyKey\"");
    }

    [Fact]
    public async Task Receipt_Page_Should_Render_Compact_Multi_Line_Layout_Without_Duplicate_Selects()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-RC"),
            Name = "Receipt Compact Warehouse"
        });
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-RC"),
            Name = "Receipt Compact Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Receipt.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        var sharedScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js"));

        html.ShouldContain("vpl-line-editor-table inventory-receipt-lines-table");
        html.ShouldContain("form-select form-select-sm");
        html.ShouldContain("form-control form-control-sm");
        html.ShouldContain("vpl-line-editor-icon-button");
        CountOccurrences(html, "name=\"Input.Lines[0].StockItemId\"").ShouldBe(1);
        CountOccurrences(html, localizer["Select"].Value).ShouldBeGreaterThanOrEqualTo(2);

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-inventory-line-container");
        pageSource.ShouldContain("data-inventory-line-row");
        pageSource.ShouldContain("data-add-button=\"#add-receipt-line\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].StockItemId\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].Quantity\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].LotNo\"");
        pageSource.ShouldContain("data-name=\"ReceivedAtTexts[__index__]\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].UnitCost\"");

        scriptSource.ShouldContain("reindexRows(container)");
        scriptSource.ShouldContain("initializeSelects(row)");
        scriptSource.ShouldContain("data-remove-line");
        sharedScriptSource.ShouldContain("setControlsDisabled(template, true)");
        sharedScriptSource.ShouldContain("setControlsDisabled(clone, false)");
    }

    [Fact]
    public async Task Posting_Pages_Should_Render_Multi_Line_Ui_Hidden_Idempotency_And_Vietnamese_Dates()
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-P"),
            Name = "Posting Warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-P"),
            Name = "Posting Component",
            Unit = "pcs"
        });

        var receiptHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));
        var issueHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Issue"));
        var adjustmentHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Adjustment"));

        foreach (var html in new[] { receiptHtml, issueHtml, adjustmentHtml })
        {
            html.ShouldContain($"{warehouse.Code} - {warehouse.Name}");
            html.ShouldContain($"{component.Code} - {component.Name}");
            html.ShouldContain("type=\"hidden\"");
            html.ShouldContain("data-inventory-posting-form");
            html.ShouldContain("data-inventory-line-container");
            html.ShouldContain("data-remove-line");
            html.ShouldContain("Thêm dòng");
            html.ShouldNotContain("type=\"text\" id=\"Input_IdempotencyKey\"");
            html.ShouldNotContain("type=\"text\" id=\"IdempotencyKey\"");
        }

        receiptHtml.ShouldContain("name=\"ReceivedAtTexts[0]\"");
        receiptHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        adjustmentHtml.ShouldContain("name=\"IncreaseReceivedAtTexts[0]\"");
        adjustmentHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
    }

    [Fact]
    public async Task Posting_Pages_Should_Register_External_Abp_Script_And_Avoid_Inline_Scripts()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Inventory/Receipt.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Issue.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldContain("@section scripts");
            pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
            pageSource.ShouldContain("<abp-script src=\"/Pages/Inventory/Posting.js\" />");
            pageSource.ShouldNotContain("<script>");
            pageSource.ShouldNotContain("<script src=");
            pageSource.ShouldNotContain("selected=\"@(option.Value");
        }

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.confirmed");
        scriptSource.ShouldContain("initializeSelects(row)");
        scriptSource.ShouldContain("vplDynamicRowSelects");
    }

    [Fact]
    public async Task Warehouses_Page_Should_Render_Create_Form_Inputs_For_Authorized_User()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Warehouses"));

        html.ShouldContain("data-warehouse-create-form");
        html.ShouldContain("name=\"NewWarehouse.Code\"");
        html.ShouldContain("name=\"NewWarehouse.Name\"");
        html.ShouldContain("name=\"NewWarehouse.Address\"");
        html.ShouldContain("type=\"text\"");
        html.ShouldContain("form-label");
        html.ShouldContain(localizer["Inventory:Code"].Value);
        html.ShouldContain(localizer["Inventory:Name"].Value);
        html.ShouldContain(localizer["Inventory:Address"].Value);
        html.ShouldContain(localizer["Create"].Value);
    }

    [Fact]
    public async Task WarehousesModel_OnPostAsync_Should_Create_Warehouse()
    {
        var code = Unique("WH-UI");
        var model = new WarehousesModel(GetRequiredService<IWarehouseAppService>())
        {
            NewWarehouse = new CreateWarehouseDto
            {
                Code = code,
                Name = "Warehouse UI Test",
                Address = "UAT address"
            }
        };

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<RedirectToPageResult>();
        model.StatusMessageKey.ShouldBe("Inventory:WarehouseCreatedSuccessfully");
    }

    [Fact]
    public async Task Warehouses_Page_Should_Register_External_Script_And_Action_Safety_Hooks()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Warehouses.cshtml"));

        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Inventory/Warehouses.js\" />");
        pageSource.ShouldContain("data-warehouse-create-form");
        pageSource.ShouldContain("form-label");
        pageSource.ShouldContain("asp-validation-summary");
        pageSource.ShouldContain("data-warehouses-page");
        pageSource.ShouldContain("data-status-success");
        pageSource.ShouldContain("data-warehouse-status-form");
        pageSource.ShouldContain("data-confirm-message");
        pageSource.ShouldContain("Inventory:ConfirmActivateWarehouse");
        pageSource.ShouldContain("Inventory:ConfirmDeactivateWarehouse");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
        pageSource.ShouldNotContain("<abp-button href=");
        pageSource.ShouldNotContain("href=\"/");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Warehouses.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.statusSuccess");
        scriptSource.ShouldContain("data-warehouse-status-form");
    }

    [Fact]
    public void InventoryPostingUi_Should_Parse_And_Format_Vietnamese_Dates()
    {
        InventoryPostingUi.FormatDate(new DateTime(2026, 6, 18)).ShouldBe("18/06/2026");
        InventoryPostingUi.TryParseDate("18/06/2026", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(new DateTime(2026, 6, 18));
        InventoryPostingUi.TryParseDate("18-06-2026", out _).ShouldBeFalse();
        InventoryPostingUi.TryParseDate(string.Empty, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task InventoryPostingUi_Should_Request_Active_Inventory_Enabled_Component_Selector_Data()
    {
        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>());

        var stockItems = Substitute.For<IStockItemAppService>();
        stockItems.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<StockItemDto>());

        await InventoryPostingUi.LoadSelectorOptionsAsync(warehouses, stockItems);

        await stockItems.Received(1).GetListAsync(Arg.Is<GetInventoryListInput>(input =>
            input.Status == InventoryEntityStatus.Active &&
            input.ItemType == StockItemType.Component &&
            input.IsInventoryEnabled == true &&
            input.MaxResultCount == LimitedResultRequestDto.MaxMaxResultCount));
    }

    [Fact]
    public void Inventory_TransactionType_Localization_Should_Map_All_Enum_Names()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        foreach (InventoryTransactionType type in Enum.GetValues<InventoryTransactionType>())
        {
            var key = $"Inventory:TransactionType:{type}";
            var label = localizer[key].Value;

            label.ShouldNotBeNullOrWhiteSpace();
            label.ShouldNotBe(key);
            label.ShouldNotBe(type.ToString());
        }
    }

    [Fact]
    public async Task Ledger_Page_Should_Render_Localized_Transaction_Type_Labels()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-TT");
        var transactions = GetRequiredService<IInventoryTransactionAppService>();

        await transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 10,
                    LotNo = Unique("LOT"),
                    UnitCost = 30000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        await transactions.PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 2 }]
        });
        await transactions.PostAdjustmentAsync(new PostAdjustmentDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentDecrease,
            Reason = "Damage",
            DecreaseLines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 1 }]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Ledger"));

        html.ShouldContain(localizer["Inventory:TransactionType:PurchaseReceipt"].Value);
        html.ShouldContain(localizer["Inventory:TransactionType:SalesIssue"].Value);
        html.ShouldContain(localizer["Inventory:TransactionType:AdjustmentDecrease"].Value);
        html.ShouldNotContain("Inventory:TransactionType:PurchaseReceipt");
        html.ShouldNotContain("Inventory:TransactionType:SalesIssue");
        html.ShouldNotContain("Inventory:TransactionType:AdjustmentDecrease");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml"));
        pageSource.ShouldContain("@L[$\"Inventory:TransactionType:{x.Type}\"]");
    }

    [Fact]
    public async Task Lots_Page_Should_Format_ReceivedAt_As_Vietnamese_Date()
    {
        var context = await CreateInquiryFilterContextAsync("LOT-DT");
        var receivedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 5,
                    LotNo = Unique("LOT-DT"),
                    UnitCost = 25000,
                    ReceivedAt = receivedAt
                }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Lots?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));

        html.ShouldContain(InventoryPostingUi.FormatDate(receivedAt));
        html.ShouldNotContain(receivedAt.ToString("O"));
    }

    [Fact]
    public async Task Inquiry_Pages_Should_Format_Money_And_Quantity_For_Vietnamese_Display()
    {
        var context = await CreateInquiryFilterContextAsync("FMT-VN");
        var receivedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var expectedMoney = 41000m.ToString("#,0", vi) + " ₫";
        var expectedQuantity = 7.25m.ToString("0.####", vi);
        var expectedInventoryValue = decimal.Round(7.25m * 41000m, 0, MidpointRounding.AwayFromZero).ToString("#,0", vi) + " ₫";

        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 7.25m,
                    LotNo = Unique("LOT-FMT"),
                    UnitCost = 41000,
                    ReceivedAt = receivedAt
                }
            ]
        });

        var lotsHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Lots?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        lotsHtml.ShouldContain(expectedMoney);
        lotsHtml.ShouldContain(expectedQuantity);
        lotsHtml.ShouldNotContain("41000.000000");
        lotsHtml.ShouldNotContain("7.2500");

        var balancesHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Balances?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        balancesHtml.ShouldContain(expectedInventoryValue);
        balancesHtml.ShouldContain(expectedQuantity);

        await GetRequiredService<IInventoryTransactionAppService>().PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 2 }]
        });

        var ledgerHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        ledgerHtml.ShouldContain((41000m * 2).ToString("#,0", vi) + " ₫");
        ledgerHtml.ShouldNotContain("82000.000000");
        ledgerHtml.ShouldNotContain("T00:00:00");

        var ledgerSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml"));
        ledgerSource.ShouldContain("InventoryPostingUi.FormatDate(postedAt)");
    }

    [Theory]
    [InlineData("/Inventory/Balances", "Inventory:NoBalances")]
    [InlineData("/Inventory/Lots", "Inventory:NoLots")]
    [InlineData("/Inventory/Ledger", "Inventory:NoLedgerEntries")]
    public async Task Inquiry_Pages_Should_Render_Empty_State_When_No_Rows(string route, string emptyStateKey)
    {
        var context = await CreateInquiryFilterContextAsync("INQ-EMPTY");
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"{route}?WarehouseId={context.WarehouseId}"));

        html.ShouldContain(localizer[emptyStateKey].Value);
        html.ShouldContain("vpl-empty-state");
    }

    [Theory]
    [InlineData("/Inventory/Balances", "/Inventory/Balances")]
    [InlineData("/Inventory/Lots", "/Inventory/Lots")]
    [InlineData("/Inventory/Ledger", "/Inventory/Ledger")]
    public async Task Inquiry_Pages_Should_Render_Warehouse_And_StockItem_Filter_Form(
        string route,
        string clearRoute)
    {
        var context = await CreateInquiryFilterContextAsync("INQ-R");
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(route));

        html.ShouldContain("method=\"get\"");
        html.ShouldContain("name=\"WarehouseId\"");
        html.ShouldContain("name=\"StockItemId\"");
        html.ShouldContain($"{context.WarehouseCode} - {context.WarehouseName}");
        html.ShouldContain($"{context.StockItemCode} - {context.StockItemName}");
        html.ShouldContain(localizer["Inventory:ApplyFilter"].Value);
        html.ShouldContain($"href=\"{clearRoute}\"");
        html.ShouldContain(localizer["Inventory:ClearFilter"].Value);
    }

    [Theory]
    [InlineData("/Inventory/Balances")]
    [InlineData("/Inventory/Lots")]
    [InlineData("/Inventory/Ledger")]
    public async Task Inquiry_Pages_Should_Preserve_Selected_Filters_From_Query_String(string route)
    {
        var context = await CreateInquiryFilterContextAsync("INQ-Q");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"{route}?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));

        AssertSelectHasSelectedValue(html, "WarehouseId", context.WarehouseId);
        AssertSelectHasSelectedValue(html, "StockItemId", context.StockItemId);
    }

    [Theory]
    [InlineData("/Inventory/Balances", "/Inventory/Balances")]
    [InlineData("/Inventory/Lots", "/Inventory/Lots")]
    [InlineData("/Inventory/Ledger", "/Inventory/Ledger")]
    public async Task Inquiry_Pages_Clear_Filter_Should_Link_Without_Query_String(
        string route,
        string clearRoute)
    {
        var context = await CreateInquiryFilterContextAsync("INQ-C");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"{route}?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));

        html.ShouldContain($"href=\"{clearRoute}\"");
        html.ShouldNotContain($"href=\"{clearRoute}?");
    }

    [Fact]
    public async Task BalancesModel_Should_Pass_Selected_Filters_To_GetBalancesAsync()
    {
        var query = Substitute.For<IInventoryQueryAppService>();
        query.GetBalancesAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new List<InventoryBalanceDto>());

        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>());

        var stockItems = Substitute.For<IStockItemAppService>();
        stockItems.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<StockItemDto>());

        var warehouseId = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var model = new BalancesModel(query, warehouses, stockItems)
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        await model.OnGetAsync();

        await query.Received(1).GetBalancesAsync(warehouseId, stockItemId);
    }

    [Fact]
    public async Task LotsModel_Should_Pass_Selected_Filters_To_GetLotsAsync()
    {
        var query = Substitute.For<IInventoryQueryAppService>();
        query.GetLotsAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new List<InventoryLotDto>());

        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>());

        var stockItems = Substitute.For<IStockItemAppService>();
        stockItems.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<StockItemDto>());

        var warehouseId = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var model = new LotsModel(query, warehouses, stockItems)
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        await model.OnGetAsync();

        await query.Received(1).GetLotsAsync(warehouseId, stockItemId);
    }

    [Fact]
    public async Task LedgerModel_Should_Pass_Selected_Filters_To_GetLedgerAsync()
    {
        var query = Substitute.For<IInventoryQueryAppService>();
        query.GetLedgerAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new List<InventoryTransactionDto>());

        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>());

        var stockItems = Substitute.For<IStockItemAppService>();
        stockItems.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<StockItemDto>());

        var warehouseId = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var model = new LedgerModel(query, warehouses, stockItems)
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        await model.OnGetAsync();

        await query.Received(1).GetLedgerAsync(warehouseId, stockItemId);
    }

    private async Task<(Guid WarehouseId, string WarehouseCode, string WarehouseName, Guid StockItemId, string StockItemCode, string StockItemName)> CreateInquiryFilterContextAsync(string prefix)
    {
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique(prefix),
            Name = "Inquiry Filter Warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique($"{prefix}-C"),
            Name = "Inquiry Filter Component",
            Unit = "pcs"
        });
        var stockItem = (await GetRequiredService<IStockItemRepository>()
            .FindByCatalogItemAsync(StockItemType.Component, component.Id))!;

        return (
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            stockItem.Id,
            stockItem.CodeSnapshot,
            stockItem.NameSnapshot);
    }

    private static void AssertSelectHasSelectedValue(string html, string selectName, Guid id)
    {
        var nameIndex = html.IndexOf($"name=\"{selectName}\"", StringComparison.OrdinalIgnoreCase);
        nameIndex.ShouldBeGreaterThan(-1, $"Expected select name=\"{selectName}\".");

        var selectStart = html.LastIndexOf("<select", nameIndex, StringComparison.OrdinalIgnoreCase);
        selectStart.ShouldBeGreaterThan(-1);

        var selectEnd = html.IndexOf("</select>", nameIndex, StringComparison.OrdinalIgnoreCase);
        selectEnd.ShouldBeGreaterThan(nameIndex);

        AssertOptionIsSelected(html[selectStart..selectEnd], id);
    }

    private static void AssertOptionIsSelected(string selectHtml, Guid id)
    {
        var valueIndex = selectHtml.IndexOf($"value=\"{id:D}\"", StringComparison.OrdinalIgnoreCase);
        valueIndex.ShouldBeGreaterThan(-1, $"Expected option value for {id:D} in select markup.");

        var optionStart = selectHtml.LastIndexOf("<option", valueIndex, StringComparison.OrdinalIgnoreCase);
        optionStart.ShouldBeGreaterThan(-1);

        var optionEnd = selectHtml.IndexOf("</option>", valueIndex, StringComparison.OrdinalIgnoreCase);
        optionEnd.ShouldBeGreaterThan(valueIndex);

        var optionMarkup = selectHtml[optionStart..optionEnd];
        optionMarkup.Contains("selected", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
            $"Expected option value for {id:D} to be selected.");
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
