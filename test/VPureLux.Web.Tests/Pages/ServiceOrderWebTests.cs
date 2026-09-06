using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Service;
using VPureLux.Warranty;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.UI.Navigation;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public partial class ServiceOrderWebTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Disabled_feature_should_hide_service_menu_and_block_order_page()
    {
        (await GetRequiredService<IMenuManager>().GetAsync(StandardMenus.Main))
            .Items.Any(item => item.Name == "VPureLux.Service").ShouldBeFalse();

        var response = await Client.GetAsync("/Service");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=404");
    }

    [Fact]
    public async Task Create_should_require_antiforgery_and_accept_vi_decimal_with_safe_output()
    {
        Enable();
        var fixture = await CreateFixtureAsync("WEB-ORDER", "<script>alert(1)</script>");

        using (var missing = await Client.PostAsync("/Service/Create", new FormUrlEncodedContent(new Dictionary<string, string>())))
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.Found);
            missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        }

        var get = await Client.GetAsync("/Service/Create");
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        var form = new Dictionary<string, string>
        {
            ["Input.CustomerAssetId"] = fixture.AssetId.ToString(),
            ["Input.AssetLabel"] = "Customer | Asset",
            ["Input.WarehouseId"] = fixture.WarehouseId.ToString(),
            ["Input.OrderDate"] = "2026-09-07",
            ["Input.ServiceAddress"] = "Safe address",
            ["Input.Note"] = "<b>operator note</b>",
            ["Input.Lines[0].LineType"] = ((byte)ServiceOrderLineType.Labor).ToString(),
            ["Input.Lines[0].CatalogItemId"] = fixture.WorkId.ToString(),
            ["Input.Lines[0].Quantity"] = "2",
            ["Input.Lines[0].UnitPrice"] = "123,50",
            ["Input.Lines[0].ItemLabel"] = "Selected work"
        };
        AddAntiforgery(get, form);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Service/Create")
        {
            Content = new FormUrlEncodedContent(form)
        };
        AddCookies(get, request);
        var result = await Client.SendAsync(request);
        result.StatusCode.ShouldBe(HttpStatusCode.Found, await result.Content.ReadAsStringAsync());

        var order = (await GetRequiredService<IServiceOrderAppService>().GetListAsync(new()
        {
            SearchText = fixture.AssetNo
        })).Items.Single();
        var details = await Client.GetAsync($"/Service/Details/{order.Id}");
        var markup = await details.Content.ReadAsStringAsync();
        details.StatusCode.ShouldBe(HttpStatusCode.OK, markup);
        markup.ShouldContain("&lt;script&gt;alert(1)&lt;/script&gt;");
        markup.ShouldContain("&lt;b&gt;operator note&lt;/b&gt;");
        markup.ShouldNotContain("<script>alert(1)</script>");

        var saved = await GetRequiredService<IServiceOrderAppService>().GetAsync(order.Id);
        saved.Lines.Single().PlannedQuantity.ShouldBe(2);
        saved.Lines.Single().UnitPrice.ShouldBe(123.50m);
    }

    [Fact]
    public async Task Confirm_should_reject_missing_token_and_accept_current_token()
    {
        Enable();
        var fixture = await CreateFixtureAsync("WEB-CONFIRM", "Service work");
        var service = GetRequiredService<IServiceOrderAppService>();
        var order = await service.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId,
            WarehouseId = fixture.WarehouseId,
            Lines = [new ServiceOrderLineInput
            {
                LineType = ServiceOrderLineType.Labor,
                CatalogItemId = fixture.WorkId,
                Quantity = 1,
                UnitPrice = 100
            }]
        });
        var url = $"/Service/Details/{order.Id}?handler=Confirm";

        using (var missing = await Client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
               {
                   ["concurrencyStamp"] = order.ConcurrencyStamp
               })))
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.Found);
            missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        }

        var details = await Client.GetAsync($"/Service/Details/{order.Id}");
        var input = new Dictionary<string, string> { ["concurrencyStamp"] = order.ConcurrencyStamp };
        AddAntiforgery(details, input);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(input)
        };
        AddCookies(details, request);
        var confirmed = await Client.SendAsync(request);

        confirmed.StatusCode.ShouldBe(HttpStatusCode.OK, await confirmed.Content.ReadAsStringAsync());
        (await service.GetAsync(order.Id)).Status.ShouldBe(ServiceOrderStatus.Confirmed);
    }

    [Theory]
    [InlineData("/Service/Edit/8bb27b4f-5828-4eac-9a8a-44ddbe4dd618")]
    [InlineData("/Service/Details/8bb27b4f-5828-4eac-9a8a-44ddbe4dd618?handler=Start")]
    [InlineData("/Service/CancelModal?Id=8bb27b4f-5828-4eac-9a8a-44ddbe4dd618")]
    [InlineData("/Service/CompleteModal?Id=8bb27b4f-5828-4eac-9a8a-44ddbe4dd618")]
    public async Task Remaining_mutation_routes_should_reject_missing_antiforgery_token(string url)
    {
        Enable();

        var response = await Client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
    }

    [Fact]
    public async Task Create_page_should_enforce_permission_then_render_when_allowed()
    {
        Enable();
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/Service/Create"))
        {
            request.Headers.Add("X-S001-Deny", VPureLuxPermissions.Service.Create);
            var denied = await Client.SendAsync(request);
            denied.IsSuccessStatusCode.ShouldBeFalse();
        }

        var allowed = await Client.GetAsync("/Service/Create");
        allowed.StatusCode.ShouldBe(HttpStatusCode.OK, await allowed.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cancel_modal_should_render_stale_concurrency_as_a_business_error()
    {
        Enable();
        var fixture = await CreateFixtureAsync("WEB-CANCEL", "Cancel work");
        var service = GetRequiredService<IServiceOrderAppService>();
        var order = await service.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId,
            WarehouseId = fixture.WarehouseId,
            Lines = [new ServiceOrderLineInput
            {
                LineType = ServiceOrderLineType.Labor,
                CatalogItemId = fixture.WorkId,
                Quantity = 1,
                UnitPrice = 100
            }]
        });
        var modalUrl = $"/Service/CancelModal?Id={order.Id}&concurrencyStamp={Uri.EscapeDataString(order.ConcurrencyStamp)}";
        var modal = await Client.GetAsync(modalUrl);
        var changed = await service.UpdateAsync(order.Id, new UpdateServiceOrderDto
        {
            OrderDate = order.OrderDate,
            Note = "Changed elsewhere",
            ConcurrencyStamp = order.ConcurrencyStamp,
            Lines = order.Lines.Select(line => new ServiceOrderLineInput
            {
                Id = line.Id,
                LineType = line.LineType,
                CatalogItemId = line.ServiceWorkId ?? line.ComponentId ?? Guid.Empty,
                Quantity = line.PlannedQuantity,
                UnitPrice = line.UnitPrice
            }).ToList()
        });
        var input = new Dictionary<string, string>
        {
            ["Id"] = order.Id.ToString(),
            ["Input.Reason"] = "Customer changed plan",
            ["Input.ConcurrencyStamp"] = order.ConcurrencyStamp
        };
        AddAntiforgery(modal, input);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/Service/CancelModal?Id={order.Id}")
        {
            Content = new FormUrlEncodedContent(input)
        };
        AddCookies(modal, request);

        var response = await Client.SendAsync(request);
        var markup = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, markup);
        markup.ShouldContain("validation-summary-errors");
        (await service.GetAsync(order.Id)).ConcurrencyStamp.ShouldBe(changed.ConcurrencyStamp);
        (await service.GetAsync(order.Id)).Status.ShouldBe(ServiceOrderStatus.Draft);
    }

    private void Enable()
    {
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
        Client.DefaultRequestHeaders.Add("X-W008-Test-Auth", "true");
    }

    private async Task<Fixture> CreateFixtureAsync(string prefix, string workName)
    {
        var group = await GetRequiredService<ICustomerGroupAppService>().CreateAsync(new CreateCustomerGroupDto
        {
            Code = Unique(prefix + "-G"), Name = prefix + " group"
        });
        var customer = await GetRequiredService<ICustomerAppService>().CreateAsync(new CreateCustomerDto
        {
            Code = Unique(prefix + "-C"), Name = prefix + " customer", CustomerGroupId = group.Id
        });
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique(prefix + "-W"), Name = prefix + " warehouse"
        });
        var work = await GetRequiredService<IServiceWorkAppService>().CreateAsync(new CreateUpdateServiceWorkDto
        {
            Code = Unique(prefix + "-WORK"), Name = workName, Unit = "Visit", DefaultPrice = 100
        });
        var assetId = Guid.NewGuid();
        var assetNo = Unique(prefix + "-ASSET");
        await GetRequiredService<IRepository<CustomerAsset, Guid>>().InsertAsync(
            CustomerAsset.CreateExternal(assetId, customer.Id, assetNo, customer.Code, customer.Name, prefix + " machine"),
            autoSave: true);

        return new Fixture(assetId, assetNo, warehouse.Id, work.Id);
    }

    private static void AddAntiforgery(HttpResponseMessage get, IDictionary<string, string> input)
    {
        var html = new HtmlDocument();
        html.LoadHtml(get.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        input["__RequestVerificationToken"] = html.DocumentNode
            .SelectSingleNode("//input[@name='__RequestVerificationToken']")
            .GetAttributeValue("value", string.Empty);
    }

    private static void AddCookies(HttpResponseMessage get, HttpRequestMessage request)
    {
        if (get.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(value => value.Split(';')[0])));
        }
    }

    private static string Unique(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..8];
    private sealed record Fixture(Guid AssetId, string AssetNo, Guid WarehouseId, Guid WorkId);
}

public class ServiceOrderUiSourceTests
{
    [Fact]
    public void Order_ui_should_use_server_paging_abp_dialogs_and_safe_rendering()
    {
        var root = FindRoot();
        var index = File.ReadAllText(Path.Combine(root, "src/VPureLux.Web/Pages/Service/Index.js"));
        var edit = File.ReadAllText(Path.Combine(root, "src/VPureLux.Web/Pages/Service/EditForm.js"));
        var details = File.ReadAllText(Path.Combine(root, "src/VPureLux.Web/Pages/Service/Details.js"));
        var template = File.ReadAllText(Path.Combine(root, "src/VPureLux.Web/Pages/Service/_OrderForm.cshtml"));
        var contract = File.ReadAllText(Path.Combine(root, "src/VPureLux.Application.Contracts/Service/ServiceOrderContracts.cs"));

        index.ShouldContain("serverSide: true");
        index.ShouldContain("abp.libs.datatables.createAjax");
        index.ShouldContain("render: encode");
        edit.ShouldContain("params.page || 1");
        edit.ShouldContain("handler = row.dataset.lineType === 'Material' ? 'Materials' : 'Works'");
        template.ShouldContain("data-add-service-line=\"Material\"");
        template.ShouldContain("data-add-service-line=\"Labor\"");
        template.ShouldContain("type=\"number\" min=\"1\" step=\"1\"");
        template.ShouldContain("<template id=\"ServiceLineTemplate\">");
        template.ShouldContain("disabled");
        details.ShouldContain("new abp.ModalManager");
        details.ShouldContain("RequestVerificationToken");
        details.ShouldContain("abp.message.confirm");
        contract.ShouldContain("CompleteAsync");
        contract.ShouldNotContain("CompleteLineAsync");

        var allScripts = index + edit + details;
        allScripts.ShouldNotContain("prompt(");
        allScripts.ShouldNotContain("alert(");
        allScripts.ShouldNotContain("window.confirm(");
        allScripts.ShouldNotContain("handler=Complete");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "VPureLux.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return directory!.FullName;
    }
}
