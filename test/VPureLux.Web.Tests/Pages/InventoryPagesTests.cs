using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Inventory;
using VPureLux.Localization;
using VPureLux.Suppliers;
using VPureLux.Web.Pages.Inventory;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;
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
    public async Task Inventory_Menu_Should_Expose_Direct_Submenu_Items()
    {
        var menuSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs"));
        var menuConstantsSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenus.cs"));

        menuConstantsSource.ShouldContain("InventoryLedger");
        menuConstantsSource.ShouldContain("InventoryReceipt");
        menuConstantsSource.ShouldContain("InventoryIssue");
        menuConstantsSource.ShouldContain("InventoryAdjustment");
        menuConstantsSource.ShouldContain("InventoryBalances");
        menuConstantsSource.ShouldContain("InventoryLots");
        menuSource.ShouldContain("var inventory = new ApplicationMenuItem");
        menuSource.ShouldContain("inventory.AddItem(new ApplicationMenuItem(");
        menuSource.ShouldContain("VPureLuxMenus.InventoryLedger");
        menuSource.ShouldContain("\"~/Inventory/Ledger\"");
        menuSource.ShouldContain("VPureLuxMenus.InventoryReceipt");
        menuSource.ShouldContain("\"~/Inventory/Receipt\"");
        menuSource.ShouldContain("VPureLuxMenus.InventoryIssue");
        menuSource.ShouldContain("\"~/Inventory/Issue\"");
        menuSource.ShouldContain("VPureLuxMenus.InventoryAdjustment");
        menuSource.ShouldContain("\"~/Inventory/Adjustment\"");
        menuSource.ShouldContain("VPureLuxMenus.InventoryBalances");
        menuSource.ShouldContain("\"~/Inventory/Balances\"");
        menuSource.ShouldContain("VPureLuxMenus.InventoryLots");
        menuSource.ShouldContain("\"~/Inventory/Lots\"");
        menuSource.ShouldNotContain("VPureLuxMenus.Inventory,\r\n            l[\"Menu:Inventory\"],\r\n            \"~/Inventory\"");
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
        var supplier = await GetRequiredService<ISupplierAppService>().CreateAsync(new CreateSupplierDto
        {
            Code = Unique("SUP-R"),
            Name = "Receipt Supplier"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));

        html.ShouldContain($"{warehouse.Code} - {warehouse.Name}");
        html.ShouldContain($"{component.Code} - {component.Name}");
        html.ShouldContain($"{supplier.Code} - {supplier.Name}");
        html.ShouldContain("name=\"Input.SupplierId\"");
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
        var cssSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/LineEditors.css"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        var sharedScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js"));

        html.ShouldContain("vpl-line-editor-table inventory-receipt-lines-table");
        html.ShouldContain("vpl-select2-target stock-item-id js-select2");
        html.ShouldContain("data-use-select2=\"true\"");
        html.ShouldNotContain("form-select form-select-sm");
        html.ShouldContain("form-control form-control-sm");
        html.ShouldContain("vpl-line-editor-icon-button");
        CountOccurrences(html, "name=\"Input.Lines[0].StockItemId\"").ShouldBe(1);
        CountOccurrences(html, localizer["Select"].Value).ShouldBeGreaterThanOrEqualTo(2);

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-inventory-line-container");
        pageSource.ShouldContain("data-inventory-line-row");
        pageSource.ShouldContain("data-add-button=\"#add-receipt-line\"");
        pageSource.ShouldContain("data-row-template=\"receipt-line-row-template\"");
        pageSource.ShouldContain("<template id=\"receipt-line-row-template\">");
        pageSource.ShouldContain("data-use-select2=\"true\"");
        pageSource.ShouldNotContain("data-dynamic-select2=\"disabled\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].StockItemId\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].Quantity\"");
        pageSource.ShouldContain("data-name=\"UnitCostTexts[__index__]\"");
        pageSource.ShouldContain("data-vnd-money=\"true\"");
        pageSource.ShouldContain("data-auto-lot-no");
        pageSource.ShouldContain("name=\"ReceivedAtText\"");
        pageSource.ShouldContain("Inventory:LotNoAutoGeneratedAtPost");
        pageSource.ShouldNotContain("data-name=\"Input.Lines[__index__].LotNo\"");
        pageSource.ShouldNotContain("asp-for=\"Input.LotNo\"");
        pageSource.ShouldNotContain("data-name=\"ReceivedAtTexts[__index__]\"");
        pageSource.ShouldNotContain("data-name=\"Input.Lines[__index__].UnitCost\"");
        pageSource.ShouldNotContain("overflow-y");
        pageSource.ShouldNotContain("max-height");
        pageSource.ShouldNotContain("height:");

        cssSource.ShouldContain(".vpl-line-editor.table-responsive");
        cssSource.ShouldContain("overflow: visible");
        cssSource.ShouldContain(".vpl-line-editor-col-action");
        cssSource.ShouldContain("min-width: 5.5rem");
        cssSource.ShouldContain("white-space: nowrap");
        cssSource.ShouldNotContain("overflow-y: auto");
        cssSource.ShouldNotContain("overflow-y: hidden");
        cssSource.ShouldNotContain("overflow-y: scroll");
        cssSource.ShouldNotContain("max-height:");
        scriptSource.ShouldContain("reindexRows(container)");
        scriptSource.ShouldContain("usesHtmlRowTemplate(container)");
        scriptSource.ShouldContain("prepareLineSelects(container, row)");
        scriptSource.ShouldContain("cloneInventoryRow(container)");
        scriptSource.ShouldContain("initializeVndMoneyInputs()");
        scriptSource.ShouldContain("event.target.value = formatted");
        scriptSource.ShouldContain("data-remove-line");
        sharedScriptSource.ShouldContain("setControlsDisabled(template, true)");
        sharedScriptSource.ShouldContain("setControlsDisabled(clone, false)");
    }

    [Fact]
    public async Task Issue_Page_Should_Render_Compact_Multi_Line_Layout_Without_Duplicate_Selects()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-IC"),
            Name = "Issue Compact Warehouse"
        });
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-IC"),
            Name = "Issue Compact Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Issue"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Issue.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        var sharedScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js"));

        html.ShouldContain("vpl-line-editor-table inventory-issue-lines-table");
        html.ShouldContain("vpl-select2-target stock-item-id js-select2");
        html.ShouldContain("data-use-select2=\"true\"");
        html.ShouldNotContain("form-select form-select-sm");
        html.ShouldContain("form-control form-control-sm");
        html.ShouldContain("vpl-line-editor-icon-button");
        CountOccurrences(html, "name=\"Input.Lines[0].StockItemId\"").ShouldBe(1);
        CountLiveRowsWithExactlyOneSelect(html, "<tr data-inventory-line-row data-line-editor-row>").ShouldBe(1);
        CountOccurrences(html, localizer["Select"].Value).ShouldBeGreaterThanOrEqualTo(2);
        html.ShouldNotContain("data-dynamic-row-template");
        html.ShouldNotContain("select2-container");

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-inventory-line-container");
        pageSource.ShouldContain("data-inventory-line-row");
        pageSource.ShouldContain("data-line-editor-row");
        pageSource.ShouldContain("data-add-button=\"#add-issue-line\"");
        pageSource.ShouldContain("data-row-template=\"issue-line-row-template\"");
        pageSource.ShouldContain("<template id=\"issue-line-row-template\">");
        pageSource.ShouldContain("data-use-select2=\"true\"");
        pageSource.ShouldNotContain("data-dynamic-select2=\"disabled\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].StockItemId\"");
        pageSource.ShouldContain("data-name=\"Input.Lines[__index__].Quantity\"");
        pageSource.ShouldContain("vpl-line-editor-col-main");
        pageSource.ShouldContain("vpl-line-editor-col-number");
        pageSource.ShouldContain("vpl-line-editor-col-action");

        scriptSource.ShouldContain("reindexRows(container)");
        scriptSource.ShouldContain("usesHtmlRowTemplate(container)");
        scriptSource.ShouldContain("prepareLineSelects(container, row)");
        scriptSource.ShouldContain("cloneInventoryRow(container)");
        scriptSource.ShouldContain("data-remove-line");
        sharedScriptSource.ShouldContain("setControlsDisabled(template, true)");
        sharedScriptSource.ShouldContain("setControlsDisabled(clone, false)");
    }

    [Fact]
    public async Task Adjustment_Page_Should_Render_Count_First_Multi_Line_Layout()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-AC"),
            Name = "Adjustment Compact Warehouse"
        });
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-AC"),
            Name = "Adjustment Compact Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Adjustment"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));
        var sharedScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js"));

        html.ShouldContain("vpl-line-editor-table inventory-adjustment-count-lines-table");
        html.ShouldContain("vpl-select2-target stock-item-id js-select2");
        html.ShouldContain("data-use-select2=\"true\"");
        html.ShouldNotContain("form-select form-select-sm");
        html.ShouldContain("form-control form-control-sm");
        html.ShouldContain("vpl-line-editor-icon-button");
        CountOccurrences(html, "name=\"CountLines[0].StockItemId\"").ShouldBe(1);
        CountOccurrences(html, "name=\"CountLines[0].CurrentQuantity\"").ShouldBe(1);
        CountOccurrences(html, "name=\"CountLines[0].CountedQuantity\"").ShouldBe(1);
        CountOccurrences(html, "name=\"CountLines[0].Delta\"").ShouldBe(1);
        CountLiveRowsWithExactlyOneSelect(html, "<tr data-inventory-line-row data-line-editor-row data-count-adjustment-row>").ShouldBe(1);
        CountOccurrences(html, localizer["Select"].Value).ShouldBeGreaterThanOrEqualTo(2);
        html.ShouldContain(localizer["Inventory:CurrentQuantity"].Value);
        html.ShouldContain(localizer["Inventory:CountedQuantity"].Value);
        html.ShouldContain(localizer["Inventory:Delta"].Value);
        html.ShouldContain(localizer["Inventory:Direction"].Value);
        html.ShouldContain(localizer["Inventory:AdjustmentReasonCategory"].Value);
        html.ShouldContain(localizer["Inventory:AdjustmentReasonDetail"].Value);
        html.ShouldContain("name=\"ReasonCategory\"");
        html.ShouldContain("name=\"ReasonDetail\"");
        html.ShouldContain(AdjustmentModel.OtherReasonCategory);
        html.ShouldContain(localizer["Inventory:NoChange"].Value);
        html.ShouldContain("name=\"CountLines[0].ReceivedAtText\"");
        html.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        html.ShouldNotContain("data-dynamic-row-template");
        html.ShouldNotContain("select2-container");

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-count-adjustment-page");
        pageSource.ShouldContain("adjustment-balance-data");
        pageSource.ShouldContain("data-inventory-line-container");
        pageSource.ShouldContain("data-inventory-line-row");
        pageSource.ShouldContain("data-line-editor-row");
        pageSource.ShouldContain("data-add-button=\"#add-adjustment-count-line\"");
        pageSource.ShouldContain("data-row-template=\"adjustment-count-line-row-template\"");
        pageSource.ShouldContain("<template id=\"adjustment-count-line-row-template\">");
        pageSource.ShouldContain("data-use-select2=\"true\"");
        pageSource.ShouldNotContain("data-dynamic-select2=\"disabled\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].StockItemId\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].CurrentQuantity\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].CountedQuantity\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].Delta\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].ReceivedAtText\"");
        pageSource.ShouldContain("data-name=\"CountLines[__index__].UnitCost\"");
        pageSource.ShouldContain("Inventory:LotNoAutoGeneratedOnSave");
        pageSource.ShouldNotContain("data-name=\"CountLines[__index__].LotNo\"");

        scriptSource.ShouldContain("reindexRows(container)");
        scriptSource.ShouldContain("usesHtmlRowTemplate(container)");
        scriptSource.ShouldContain("prepareLineSelects(container, row)");
        scriptSource.ShouldContain("cloneInventoryRow(container)");
        scriptSource.ShouldContain("data-remove-line");
        scriptSource.ShouldContain("initializeCountAdjustment(page)");
        scriptSource.ShouldContain("data-current-quantity");
        scriptSource.ShouldContain("data-delta");
        scriptSource.ShouldContain("data-positive-delta-field");
        sharedScriptSource.ShouldContain("setControlsDisabled(template, true)");
        sharedScriptSource.ShouldContain("setControlsDisabled(clone, false)");
    }

    [Fact]
    public async Task Adjustment_Count_First_Positive_Delta_Should_Post_Increase()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateInquiryFilterContextAsync("ADJ-POS");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.ReasonDetail = "Count increase";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 3,
                ReceivedAtText = "18/06/2026",
                UnitCost = 12000
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<RedirectToPageResult>();
        var transaction = (await GetRequiredService<IInventoryQueryAppService>()
                .GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .Single(x => x.Type == InventoryTransactionType.AdjustmentIncrease);
        transaction.Reason.ShouldBe("Kiểm kê lệch tồn - Count increase");
        transaction.Lines.Single().Quantity.ShouldBe(3);
        transaction.Lines.Single().Direction.ShouldBe(InventoryMovementDirection.Increase);
        transaction.Lines.Single().UnitCost.ShouldBe(12000);
        transaction.Lines.Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
    }

    [Fact]
    public async Task Adjustment_Count_First_Negative_Delta_Should_Post_Decrease()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateInquiryFilterContextAsync("ADJ-NEG");
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
                    LotNo = Unique("LOT-ADJ-NEG"),
                    UnitCost = 10000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Hàng hỏng";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 2
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<RedirectToPageResult>();
        var transaction = (await GetRequiredService<IInventoryQueryAppService>()
                .GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .Single(x => x.Type == InventoryTransactionType.AdjustmentDecrease);
        transaction.Reason.ShouldBe("Hàng hỏng");
        transaction.Lines.Single().Quantity.ShouldBe(3);
        transaction.Lines.Single().Direction.ShouldBe(InventoryMovementDirection.Decrease);
        transaction.Lines.Single().LotNo.ShouldBeNull();
        (await GetRequiredService<IDistributedCache>().GetStringAsync($"Sequence:InventoryLot:{DatePart()}")).ShouldBeNull();
    }

    [Fact]
    public async Task Adjustment_Count_First_Mixed_Directions_Should_Be_Blocked_Before_Posting()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-MIX");
        var secondComponent = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("ADJ-MIX-C2"),
            Name = "Second Adjustment Component",
            Unit = "pcs"
        });
        var secondStockItem = (await GetRequiredService<IStockItemRepository>()
            .FindByCatalogItemAsync(StockItemType.Component, secondComponent.Id))!;
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
                    LotNo = Unique("LOT-ADJ-MIX"),
                    UnitCost = 10000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.ReasonDetail = "Mixed blocked";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 2
            },
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = secondStockItem.Id,
                CountedQuantity = 3,
                LotNo = Unique("LOT-ADJ-MIX-INC"),
                ReceivedAtText = "18/06/2026",
                UnitCost = 12000
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:AdjustmentMixedDirectionsNotAtomic"].Value);
        var ledger = await GetRequiredService<IInventoryQueryAppService>().GetLedgerAsync(context.WarehouseId);
        ledger.Any(x =>
                x.Reason == "Kiểm kê lệch tồn - Mixed blocked" &&
                x.Type is InventoryTransactionType.AdjustmentIncrease or InventoryTransactionType.AdjustmentDecrease)
            .ShouldBeFalse();
        ledger.Count.ShouldBe(1);
        ledger.Single().Type.ShouldBe(InventoryTransactionType.PurchaseReceipt);
    }

    [Fact]
    public async Task Adjustment_Count_First_All_Zero_Delta_Should_Be_Blocked()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateInquiryFilterContextAsync("ADJ-ZERO");
        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 4,
                    LotNo = Unique("LOT-ADJ-ZERO"),
                    UnitCost = 10000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 4
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:AdjustmentAllRowsZeroDelta"].Value);
        (await GetRequiredService<IDistributedCache>().GetStringAsync($"Sequence:InventoryLot:{DatePart()}")).ShouldBeNull();
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Require_Reason_Category()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-RSN");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = string.Empty;
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 1,
                LotNo = Unique("LOT-ADJ-RSN"),
                ReceivedAtText = "18/06/2026",
                UnitCost = 10000
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState[nameof(AdjustmentModel.ReasonCategory)]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:AdjustmentReasonCategoryRequired"].Value);
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Require_Detail_For_Other_Reason()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-OTH");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = AdjustmentModel.OtherReasonCategory;
        model.ReasonDetail = string.Empty;
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 1,
                LotNo = Unique("LOT-ADJ-OTH"),
                ReceivedAtText = "18/06/2026",
                UnitCost = 10000
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState[nameof(AdjustmentModel.ReasonDetail)]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:AdjustmentReasonDetailRequiredForOther"].Value);
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Compose_Other_Reason_With_Detail()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-OTH-C");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = AdjustmentModel.OtherReasonCategory;
        model.ReasonDetail = "Stock card cleanup";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 2,
                LotNo = Unique("LOT-ADJ-OTH-C"),
                ReceivedAtText = "18/06/2026",
                UnitCost = 10000
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<RedirectToPageResult>();
        var transaction = (await GetRequiredService<IInventoryQueryAppService>()
                .GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .Single(x => x.Type == InventoryTransactionType.AdjustmentIncrease);
        transaction.Reason.ShouldBe("Khác - Stock card cleanup");
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Keep_Base_Input_Validation_Friendly()
    {
        var model = CreateAdjustmentModel();
        model.WarehouseId = Guid.Empty;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput()
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        model.ModelState[nameof(AdjustmentModel.WarehouseId)]!.Errors.Single().ErrorMessage
            .ShouldContain(localizer["Inventory:WarehouseRequired"].Value);
        model.ModelState["CountLines[0].StockItemId"]!.Errors.Single().ErrorMessage
            .ShouldContain(localizer["Inventory:StockItemRequired"].Value);
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Require_Counted_Quantity()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-CNT-QTY");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState["CountLines[0].CountedQuantity"]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:CountedQuantityRequired"].Value);
    }

    [Fact]
    public async Task Adjustment_Count_First_Should_Block_Negative_Counted_Quantity()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-NEG-QTY");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = -1
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState["CountLines[0].CountedQuantity"]!.Errors.Single().ErrorMessage
            .ShouldContain(GetRequiredService<IStringLocalizer<VPureLuxResource>>()["Inventory:CountedQuantityNonNegative"].Value);
    }

    [Fact]
    public async Task Adjustment_Count_First_Positive_Delta_Should_Require_Valuation_Inputs()
    {
        var context = await CreateInquiryFilterContextAsync("ADJ-VAL");
        var model = CreateAdjustmentModel();
        model.WarehouseId = context.WarehouseId;
        model.ReasonCategory = "Kiểm kê lệch tồn";
        model.ReasonDetail = "Missing valuation";
        model.CountLines =
        [
            new AdjustmentModel.CountAdjustmentLineInput
            {
                StockItemId = context.StockItemId,
                CountedQuantity = 1,
                ReceivedAtText = "not-a-date"
            }
        ];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.ContainsKey("CountLines[0].LotNo").ShouldBeFalse();
        model.ModelState["CountLines[0].UnitCost"]!.Errors.ShouldNotBeEmpty();
        model.ModelState["CountLines[0].ReceivedAtText"]!.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Receipt_Page_Should_Show_Auto_Generated_LotNo_Hint_Without_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-LOT-UI"),
            Name = "Receipt Lot UI Warehouse"
        });
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-LOT-UI"),
            Name = "Receipt Lot UI Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Receipt.cshtml"));
        var autoGeneratedHint = localizer["Inventory:LotNoAutoGeneratedAtPost"].Value;

        html.ShouldContain(autoGeneratedHint);
        html.ShouldContain("data-auto-lot-no");
        html.ShouldNotContain("name=\"Input.LotNo\"");
        html.ShouldNotContain("name=\"Input.Lines[0].LotNo\"");
        pageSource.ShouldContain("Inventory:LotNoAutoGeneratedAtPost");
        pageSource.ShouldNotContain("asp-for=\"Input.LotNo\"");
        pageSource.ShouldNotContain("name=\"Input.Lines[@i].LotNo\"");
        pageSource.ShouldNotContain("data-name=\"Input.Lines[__index__].LotNo\"");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Receipt_Page_Should_Block_Non_Positive_Quantity(int quantity)
    {
        await ResetInventoryLotSequenceAsync();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("RCT-QTY");
        var model = CreateReceiptModel();
        model.Input = ReceiptInput(context.WarehouseId, context.StockItemId, quantity);
        model.ReceivedAtText = "18/06/2026";
        model.UnitCostTexts = ["100.000"];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState["Input.Lines[0].Quantity"]!.Errors.Single().ErrorMessage
            .ShouldContain(localizer["Inventory:ReceiptQuantityPositive"].Value);
        model.Input.Lines.Single().Quantity.ShouldBe(quantity);
        (await GetRequiredService<IInventoryQueryAppService>().GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .ShouldBeEmpty();
        (await GetRequiredService<IInventoryQueryAppService>().GetLotsAsync(context.WarehouseId, context.StockItemId))
            .ShouldBeEmpty();
        (await GetRequiredService<IDistributedCache>().GetStringAsync($"Sequence:InventoryLot:{DatePart()}"))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Receipt_Page_Should_Block_Missing_Quantity_With_Friendly_Field_Error()
    {
        await ResetInventoryLotSequenceAsync();
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("RCT-MISS-QTY");
        var model = CreateReceiptModel();
        model.Input = ReceiptInput(context.WarehouseId, context.StockItemId, 0);
        model.ReceivedAtText = "18/06/2026";
        model.UnitCostTexts = ["100.000"];
        model.ModelState.SetModelValue(
            "Input.Lines[0].Quantity",
            new ValueProviderResult(string.Empty));

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState["Input.Lines[0].Quantity"]!.Errors.Single().ErrorMessage
            .ShouldContain(localizer["Inventory:ReceiptQuantityPositive"].Value);
        model.GetPostedFieldValue("Input.Lines[0].Quantity", model.Input.Lines[0].Quantity).ShouldBe(string.Empty);
        (await GetRequiredService<IInventoryQueryAppService>().GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .ShouldBeEmpty();
        (await GetRequiredService<IInventoryQueryAppService>().GetLotsAsync(context.WarehouseId, context.StockItemId))
            .ShouldBeEmpty();
        (await GetRequiredService<IDistributedCache>().GetStringAsync($"Sequence:InventoryLot:{DatePart()}"))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Receipt_Page_Should_Render_Quantity_Field_Validation_Hook_And_Message()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Receipt"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Receipt.cshtml"));
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Receipt.cshtml.cs"));

        html.ShouldContain("data-valmsg-for=\"Input.Lines[0].Quantity\"");
        pageSource.ShouldContain("min=\"0.0001\"");
        pageModelSource.ShouldContain("Inventory:ReceiptQuantityPositive");
        localizer["Inventory:ReceiptQuantityPositive"].Value.ShouldBe("Số lượng nhập phải lớn hơn 0.");
    }

    [Fact]
    public async Task Receipt_Page_Should_Post_Positive_Quantity_And_Generate_LotNo_When_Blank()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateInquiryFilterContextAsync("RCT-OK-QTY");
        var model = CreateReceiptModel();
        model.Input = ReceiptInput(context.WarehouseId, context.StockItemId, 2);
        model.ReceivedAtText = "18/06/2026";
        model.UnitCostTexts = ["120.000"];

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<RedirectToPageResult>();
        var ledger = await GetRequiredService<IInventoryQueryAppService>()
            .GetLedgerAsync(context.WarehouseId, context.StockItemId);
        ledger.Single().Type.ShouldBe(InventoryTransactionType.PurchaseReceipt);
        ledger.Single().Lines.Single().Quantity.ShouldBe(2);
        ledger.Single().Lines.Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
        ledger.Single().Lines.Single().ReceivedAt!.Value.Date.ShouldBe(new DateTime(2026, 6, 18));
        ledger.Single().Lines.Single().UnitCost.ShouldBe(120000);
    }

    [Fact]
    public async Task Adjustment_Positive_Delta_Should_Show_Auto_Generated_LotNo_And_Valuation_Fields()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique("WH-ADJ-LOT"),
            Name = "Adjustment Lot UI Warehouse"
        });
        await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("CMP-ADJ-LOT"),
            Name = "Adjustment Lot UI Component",
            Unit = "pcs"
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Adjustment"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"));
        var autoGeneratedHint = localizer["Inventory:LotNoAutoGeneratedOnSave"].Value;

        html.ShouldContain(autoGeneratedHint);
        html.ShouldContain("name=\"CountLines[0].ReceivedAtText\"");
        html.ShouldContain("name=\"CountLines[0].UnitCost\"");
        html.ShouldNotContain("name=\"CountLines[0].LotNo\"");
        pageSource.ShouldContain("data-positive-delta-field");
        pageSource.ShouldContain("Inventory:LotNoAutoGeneratedOnSave");
        pageSource.ShouldNotContain("name=\"CountLines[@i].LotNo\"");
        pageSource.ShouldNotContain("data-name=\"CountLines[__index__].LotNo\"");
    }

    [Fact]
    public async Task Adjustment_Negative_Delta_Should_Not_Show_Or_Require_LotNo_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Posting.js"));

        pageSource.ShouldNotContain("name=\"CountLines[@i].LotNo\"");
        pageSource.ShouldNotContain("data-name=\"CountLines[__index__].LotNo\"");
        pageSource.ShouldContain("data-positive-delta-field");
        scriptSource.ShouldContain("setPositiveFields(row, countedQuantity !== null && currentQuantity !== null && delta > 0)");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Inventory/Adjustment"));
        html.ShouldNotContain("name=\"CountLines[0].LotNo\"");
        html.ShouldContain(localizer["Inventory:LotNoAutoGeneratedOnSave"].Value);
    }

    [Fact]
    public async Task Inventory_Line_Editor_Terminology_Should_Not_Reintroduce_Linh_Kien()
    {
        var legacyComponentText = "Linh " + "kiện";
        var legacyComponentTextLower = "linh " + "kiện";
        var sourceFiles = new[]
        {
            "src/VPureLux.Web/Pages/Inventory/Issue.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Receipt.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml",
            "src/VPureLux.Web/Pages/Inventory/Ledger.cshtml",
            "src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json"
        };

        foreach (var sourceFile in sourceFiles)
        {
            var source = await File.ReadAllTextAsync(GetRepoFilePath(sourceFile));
            source.ShouldNotContain(legacyComponentText);
            source.ShouldNotContain(legacyComponentTextLower);
        }

        var localizationSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json"));
        localizationSource.ShouldContain("\"Bom:Component\": \"Vật tư\"");
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

        receiptHtml.ShouldContain("name=\"ReceivedAtText\"");
        receiptHtml.ShouldContain("data-auto-lot-no");
        receiptHtml.ShouldNotContain("name=\"Input.LotNo\"");
        receiptHtml.ShouldContain("name=\"UnitCostTexts[0]\"");
        receiptHtml.ShouldContain("data-vnd-money=\"true\"");
        receiptHtml.ShouldContain("<span class=\"input-group-text\">₫</span>");
        receiptHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        adjustmentHtml.ShouldContain("name=\"CountLines[0].ReceivedAtText\"");
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
        scriptSource.ShouldContain("stripLeptonXSelectEnhancements(row)");
        scriptSource.ShouldContain("initializeSelects(row, '.stock-item-id')");
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
        var rows = await GetLedgerRowsAsync();

        html.ShouldContain(localizer["Inventory:TransactionType:PurchaseReceipt"].Value);
        html.ShouldContain(localizer["Inventory:TransactionType:SalesIssue"].Value);
        html.ShouldContain(localizer["Inventory:TransactionType:AdjustmentDecrease"].Value);
        html.ShouldNotContain("Inventory:TransactionType:PurchaseReceipt");
        html.ShouldNotContain("Inventory:TransactionType:SalesIssue");
        html.ShouldNotContain("Inventory:TransactionType:AdjustmentDecrease");
        rows.Items.Any(x => x.Type == localizer["Inventory:TransactionType:PurchaseReceipt"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.Type == localizer["Inventory:TransactionType:SalesIssue"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.Type == localizer["Inventory:TransactionType:AdjustmentDecrease"].Value).ShouldBeTrue();

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml"));
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml.cs"));
        pageSource.ShouldContain("<abp-script src=\"/Pages/Inventory/Ledger.js\" />");
        pageModelSource.ShouldContain("L[$\"Inventory:TransactionType:{row.Type}\"].Value");
    }

    [Fact]
    public async Task Ledger_Page_Should_Render_Trace_Columns_And_Line_Level_Quantities()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-TRACE");
        var transactions = GetRequiredService<IInventoryTransactionAppService>();
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var receiptSource = Unique("TRACE-REF");

        await transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            ReferenceType = receiptSource,
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 5,
                    LotNo = Unique("LOT-TRACE"),
                    UnitCost = 12000,
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

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var rows = await GetLedgerRowsAsync(context.WarehouseId, context.StockItemId);

        html.ShouldContain(localizer["Inventory:Material"].Value);
        html.ShouldContain(localizer["Inventory:SourceReference"].Value);
        html.ShouldContain(localizer["Inventory:QuantityIn"].Value);
        html.ShouldContain(localizer["Inventory:QuantityOut"].Value);
        html.ShouldContain(localizer["Inventory:UnitCost"].Value);
        html.ShouldContain(localizer["Inventory:Amount"].Value);
        rows.Items.Any(x => x.Warehouse == $"{context.WarehouseCode} - {context.WarehouseName}").ShouldBeTrue();
        rows.Items.Any(x => x.StockItem == $"{context.StockItemCode} - {context.StockItemName}").ShouldBeTrue();
        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceUnknown"].Value && x.SourceDetail!.Contains(receiptSource)).ShouldBeTrue();
        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceManualIssue"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.UnitCost == 12000m.ToString("#,0", vi) + " ₫").ShouldBeTrue();
        rows.Items.Any(x => x.Amount == 60000m.ToString("#,0", vi) + " ₫").ShouldBeTrue();
        rows.Items.Any(x => x.Amount == 24000m.ToString("#,0", vi) + " ₫").ShouldBeTrue();
        rows.Items.Any(x => x.QuantityIn == "5").ShouldBeTrue();
        rows.Items.Any(x => x.QuantityOut == "2").ShouldBeTrue();
        html.ShouldNotContain(localizer["Inventory:IssueCost"].Value);
    }

    [Fact]
    public async Task Ledger_Page_Should_Render_Friendly_Source_Labels_And_Bom_Link()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-SRC");
        var transactions = GetRequiredService<IInventoryTransactionAppService>();
        var bom = await CreateBomVersionForLedgerSourceAsync("LEDGER-SRC-BOM");
        var salesLineId = Guid.NewGuid();

        await transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 3,
                    LotNo = Unique("LOT-SRC"),
                    UnitCost = 10000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        await transactions.PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            ReferenceType = "SalesOrderLine",
            ReferenceId = salesLineId,
            BomVersionId = bom.Id,
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 1 }]
        });
        await transactions.PostAdjustmentAsync(new PostAdjustmentDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentIncrease,
            Reason = "Kiểm kê lệch tồn",
            IncreaseLines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 1,
                    LotNo = Unique("LOT-SRC-ADJ"),
                    UnitCost = 12000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var rows = await GetLedgerRowsAsync(context.WarehouseId, context.StockItemId);

        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceManualReceipt"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceSalesOrder"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceAdjustment"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.SourceDetail != null && x.SourceDetail.Contains(localizer["Inventory:SourceSalesOrderLineId"].Value)).ShouldBeTrue();
        rows.Items.Any(x => x.SourceDetail != null && x.SourceDetail.Contains(salesLineId.ToString("D"))).ShouldBeTrue();
        rows.Items.Any(x => x.SourceBomVersionId == bom.Id).ShouldBeTrue();
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.js"));
        scriptSource.ShouldContain("Bom/Details/");
        scriptSource.ShouldContain("Inventory:SourceOpenBom");
    }

    [Fact]
    public async Task Ledger_Page_Should_Render_Unknown_Source_Fallback_Without_Broken_Link()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-UNK");
        var unknownReferenceId = Guid.NewGuid();

        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            ReferenceType = "MysteryDocument",
            ReferenceId = unknownReferenceId,
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 2,
                    LotNo = Unique("LOT-UNK"),
                    UnitCost = 11000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var rows = await GetLedgerRowsAsync(context.WarehouseId, context.StockItemId);

        rows.Items.Any(x => x.SourceLabel == localizer["Inventory:SourceUnknown"].Value).ShouldBeTrue();
        rows.Items.Any(x => x.SourceDetail != null && x.SourceDetail.Contains("MysteryDocument")).ShouldBeTrue();
        rows.Items.Any(x => x.SourceDetail != null && x.SourceDetail.Contains(unknownReferenceId.ToString("D"))).ShouldBeTrue();
        html.ShouldNotContain($"/Bom/Details/{unknownReferenceId}");
    }

    [Fact]
    public async Task Ledger_Page_Should_Filter_By_Type_Date_Range_And_SourceReference()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-FLT");
        var transactions = GetRequiredService<IInventoryTransactionAppService>();
        var receiptSource = Unique("SRC-FLT");

        await transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            ReferenceType = receiptSource,
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 4,
                    LotNo = Unique("LOT-FLT"),
                    UnitCost = 15000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        await transactions.PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 1 }]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}&Type={InventoryTransactionType.PurchaseReceipt}&FromDate=2000-01-01&ToDate=2999-12-31&SourceReference={receiptSource}"));
        var rows = await GetLedgerRowsAsync(
            context.WarehouseId,
            context.StockItemId,
            InventoryTransactionType.PurchaseReceipt,
            new DateTime(2000, 1, 1),
            new DateTime(2999, 12, 31),
            receiptSource);

        html.ShouldContain("name=\"Type\"");
        html.ShouldContain("name=\"FromDate\"");
        html.ShouldContain("name=\"ToDate\"");
        html.ShouldContain("name=\"SourceReference\"");
        html.ShouldContain(localizer["Inventory:TransactionType:PurchaseReceipt"].Value);
        rows.Items.Count.ShouldBe(1);
        rows.Items.Single().SourceDetail!.ShouldContain(receiptSource);
        rows.Items.Single().Type.ShouldBe(localizer["Inventory:TransactionType:PurchaseReceipt"].Value);
        CountOccurrences(html, localizer["Inventory:TransactionType:SalesIssue"].Value).ShouldBe(1);
    }

    [Fact]
    public async Task Ledger_Page_Should_Filter_By_Friendly_Source_Label()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var context = await CreateInquiryFilterContextAsync("LEDGER-SRC-FLT");
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
                    Quantity = 4,
                    LotNo = Unique("LOT-SRC-FLT"),
                    UnitCost = 15000,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        await transactions.PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 1 }]
        });

        var source = Uri.EscapeDataString(localizer["Inventory:SourceManualReceipt"].Value);
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}&SourceReference={source}"));
        var rows = await GetLedgerRowsAsync(
            context.WarehouseId,
            context.StockItemId,
            sourceReference: localizer["Inventory:SourceManualReceipt"].Value);

        html.ShouldContain("name=\"WarehouseId\"");
        html.ShouldContain("name=\"StockItemId\"");
        html.ShouldContain("name=\"SourceReference\"");
        rows.Items.ShouldNotBeEmpty();
        rows.Items.ShouldAllBe(x => x.SourceLabel == localizer["Inventory:SourceManualReceipt"].Value);
        rows.Items.ShouldAllBe(x => x.SourceLabel != localizer["Inventory:SourceManualIssue"].Value);
    }

    [Fact]
    public async Task Ledger_Page_Should_Not_Fake_Deferred_BalanceAfter_Or_User_Fields()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml"));
        var modelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml.cs"));

        pageSource.ShouldNotContain("BalanceAfter");
        pageSource.ShouldNotContain("Inventory:User");
        pageSource.ShouldNotContain("Audit:User");
        modelSource.ShouldNotContain("BalanceAfter");
        modelSource.ShouldNotContain("UserLabel");
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
        var rows = await GetLotsRowsAsync(context.WarehouseId, context.StockItemId);

        rows.Items.Any(x => x.ReceivedAt == InventoryPostingUi.FormatDate(receivedAt)).ShouldBeTrue();
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
        var lotRows = await GetLotsRowsAsync(context.WarehouseId, context.StockItemId);
        lotRows.Items.Any(x => x.UnitCost == expectedMoney).ShouldBeTrue();
        lotRows.Items.Any(x => x.ReceivedQuantity == expectedQuantity).ShouldBeTrue();
        lotRows.Items.Any(x => x.AvailableQuantity == expectedQuantity).ShouldBeTrue();
        lotRows.Items.Any(x => x.ReceiptValue == expectedInventoryValue).ShouldBeTrue();
        lotsHtml.ShouldNotContain("41000.000000");
        lotsHtml.ShouldNotContain("7.2500");

        var balancesHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Balances?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var balanceRows = await GetBalanceRowsAsync(context.WarehouseId, context.StockItemId);
        balanceRows.Items.Any(x => x.InventoryValue == expectedInventoryValue).ShouldBeTrue();
        balanceRows.Items.Any(x => x.QuantityOnHand == expectedQuantity).ShouldBeTrue();

        await GetRequiredService<IInventoryTransactionAppService>().PostIssueAsync(new PostIssueDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 2 }]
        });

        var ledgerHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Ledger?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var ledgerRows = await GetLedgerRowsAsync(context.WarehouseId, context.StockItemId);
        ledgerRows.Items.Any(x => x.Amount == (41000m * 2).ToString("#,0", vi) + " ₫").ShouldBeTrue();
        ledgerHtml.ShouldNotContain("82000.000000");
        ledgerHtml.ShouldNotContain("T00:00:00");

        var ledgerSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Ledger.cshtml.cs"));
        ledgerSource.ShouldContain("FormatDateTime(row.PostedAt.Value)");
    }

    [Fact]
    public async Task Lots_Page_Should_Show_Receipt_Price_And_Quantity_History()
    {
        var context = await CreateInquiryFilterContextAsync("LOT-HIST");
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var vi = CultureInfo.GetCultureInfo("vi-VN");

        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 11,
                    LotNo = Unique("LOT-HIST"),
                    UnitCost = 12345,
                    ReceivedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Inventory/Lots?WarehouseId={context.WarehouseId}&StockItemId={context.StockItemId}"));
        var rows = await GetLotsRowsAsync(context.WarehouseId, context.StockItemId);

        html.ShouldContain(localizer["Inventory:ReceiptLotHistory"].Value);
        html.ShouldContain(localizer["Inventory:ReceivedQuantity"].Value);
        html.ShouldContain(localizer["Inventory:ReceiptValue"].Value);
        rows.Items.Any(x => x.ReceivedQuantity == "11").ShouldBeTrue();
        rows.Items.Any(x => x.UnitCost == 12345m.ToString("#,0", vi) + " ₫").ShouldBeTrue();
        rows.Items.Any(x => x.ReceiptValue == (12345m * 11).ToString("#,0", vi) + " ₫").ShouldBeTrue();
    }

    [Fact]
    public async Task Balances_Page_Should_Link_To_Filtered_Receipt_Lot_History()
    {
        var context = await CreateInquiryFilterContextAsync("BAL-HIST");
        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 3,
                    LotNo = Unique("LOT-BAL-HIST"),
                    UnitCost = 9000,
                    ReceivedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        });

        var rows = await GetBalanceRowsAsync(context.WarehouseId, context.StockItemId);
        rows.Items.Any(x => x.WarehouseId == context.WarehouseId && x.StockItemId == context.StockItemId)
            .ShouldBeTrue();

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Balances.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Inventory/Balances.js"));

        pageSource.ShouldContain("@L[\"Actions\"]");
        scriptSource.ShouldContain("Inventory/Lots?WarehouseId=");
        scriptSource.ShouldContain("Inventory:ViewReceiptLotHistory");
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

        await model.OnGetListAsync(new BalancesModel.InventoryInquiryListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            MaxResultCount = 10
        });

        await query.Received(1).GetBalancesAsync(warehouseId, stockItemId);
    }

    [Fact]
    public async Task LotsModel_Should_Pass_Selected_Filters_To_GetLotsAsync()
    {
        var warehouseId = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var query = Substitute.For<IInventoryQueryAppService>();
        query.GetLotsAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new List<InventoryLotDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    StockItemId = stockItemId,
                    LotNo = "LOT-1",
                    ReceivedAt = DateTime.Today,
                    ReceivedQuantity = 1,
                    AvailableQuantity = 1,
                    UnitCost = 100,
                    SupplierCode = "SUP",
                    SupplierName = "Supplier"
                }
            });

        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>());

        var stockItems = Substitute.For<IStockItemAppService>();
        stockItems.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<StockItemDto>());

        var model = new LotsModel(query, warehouses, stockItems)
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId
        };
        SetPageContext(model);

        var result = await model.OnGetListAsync(new LotsModel.InventoryInquiryListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            MaxResultCount = 10
        });

        result.Value.ShouldBeOfType<PagedResultDto<LotsModel.InventoryLotRow>>()
            .Items.Single().Supplier.ShouldBe("SUP - Supplier");
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

        await model.OnGetListAsync(new LedgerModel.LedgerListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            MaxResultCount = 10
        });

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

    private async Task<BomVersionDto> CreateBomVersionForLedgerSourceAsync(string prefix)
    {
        var product = await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique($"{prefix}-P"),
            Name = "Ledger Source Product"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique($"{prefix}-C"),
            Name = "Ledger Source Component",
            Unit = "pcs"
        });

        return await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
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
    }

    private AdjustmentModel CreateAdjustmentModel()
    {
        var model = GetRequiredService<AdjustmentModel>();
        SetPageContext(model);
        return model;
    }

    private ReceiptModel CreateReceiptModel()
    {
        var model = GetRequiredService<ReceiptModel>();
        SetPageContext(model);
        return model;
    }

    private static PostReceiptDto ReceiptInput(Guid warehouseId, Guid stockItemId, decimal quantity) => new()
    {
        WarehouseId = warehouseId,
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Lines =
        [
            new ReceiptLineInput
            {
                StockItemId = stockItemId,
                Quantity = quantity,
                UnitCost = 100
            }
        ]
    };

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

    private async Task<PagedResultDto<BalancesModel.InventoryBalanceRow>> GetBalanceRowsAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null)
    {
        var model = new BalancesModel(
            GetRequiredService<IInventoryQueryAppService>(),
            GetRequiredService<IWarehouseAppService>(),
            GetRequiredService<IStockItemAppService>());
        SetPageContext(model);

        var result = await model.OnGetListAsync(new BalancesModel.InventoryInquiryListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            MaxResultCount = 100
        });

        return result.Value.ShouldBeOfType<PagedResultDto<BalancesModel.InventoryBalanceRow>>();
    }

    private async Task<PagedResultDto<LotsModel.InventoryLotRow>> GetLotsRowsAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null)
    {
        var model = new LotsModel(
            GetRequiredService<IInventoryQueryAppService>(),
            GetRequiredService<IWarehouseAppService>(),
            GetRequiredService<IStockItemAppService>());
        SetPageContext(model);

        var result = await model.OnGetListAsync(new LotsModel.InventoryInquiryListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            MaxResultCount = 100
        });

        return result.Value.ShouldBeOfType<PagedResultDto<LotsModel.InventoryLotRow>>();
    }

    private async Task<PagedResultDto<LedgerModel.LedgerTraceListRow>> GetLedgerRowsAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null,
        InventoryTransactionType? type = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? sourceReference = null)
    {
        var model = new LedgerModel(
            GetRequiredService<IInventoryQueryAppService>(),
            GetRequiredService<IWarehouseAppService>(),
            GetRequiredService<IStockItemAppService>());
        SetPageContext(model);

        var result = await model.OnGetListAsync(new LedgerModel.LedgerListInput
        {
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate,
            SourceReference = sourceReference,
            MaxResultCount = 100
        });

        return result.Value.ShouldBeOfType<PagedResultDto<LedgerModel.LedgerTraceListRow>>();
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

    private string DatePart() => GetRequiredService<IClock>().Now.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private Task ResetInventoryLotSequenceAsync() =>
        GetRequiredService<IDistributedCache>().RemoveAsync($"Sequence:InventoryLot:{DatePart()}");

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

    private static int CountLiveRowsWithExactlyOneSelect(string html, string rowMarker)
    {
        html = RemoveTemplateBlocks(html);
        var rowCount = 0;
        var index = 0;

        while ((index = html.IndexOf(rowMarker, index, StringComparison.Ordinal)) >= 0)
        {
            var end = html.IndexOf("</tr>", index, StringComparison.Ordinal);
            end.ShouldBeGreaterThan(index);

            var rowHtml = html[index..end];
            CountOccurrences(rowHtml, "<select ").ShouldBe(1);
            CountOccurrences(rowHtml, "data-dynamic-row-template").ShouldBe(0);
            rowCount++;
            index = end + "</tr>".Length;
        }

        return rowCount;
    }

    private static string RemoveTemplateBlocks(string html)
    {
        while (true)
        {
            var templateStart = html.IndexOf("<template", StringComparison.OrdinalIgnoreCase);
            if (templateStart < 0)
            {
                return html;
            }

            var templateEnd = html.IndexOf("</template>", templateStart, StringComparison.OrdinalIgnoreCase);
            if (templateEnd < 0)
            {
                return html[..templateStart];
            }

            html = html.Remove(templateStart, templateEnd + "</template>".Length - templateStart);
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
