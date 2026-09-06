using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Service;
using VPureLux.Permissions;
using VPureLux.EntityFrameworkCore.Warranty;
using Volo.Abp.Application.Dtos;
using Volo.Abp.UI.Navigation;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ServiceWorkWebTests : VPureLuxWebTestBase
{
    private void Enable()
    {
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
        Client.DefaultRequestHeaders.Add("X-W008-Test-Auth", "true");
    }

    [Fact]
    public async Task Service_disabled_should_hide_menu_and_block_pages_and_api()
    {
        var menu = await GetRequiredService<IMenuManager>().GetAsync(StandardMenus.Main);
        menu.Items.Any(x => x.Name == "VPureLux.Service").ShouldBeFalse();
        var page = await Client.GetAsync("/Service/Works");
        page.StatusCode.ShouldBe(HttpStatusCode.Found);
        page.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=404");
        var api = await Client.GetAsync("/api/app/service-work");
        api.StatusCode.ShouldBe(HttpStatusCode.Forbidden, await api.Content.ReadAsStringAsync());
        (await api.Content.ReadAsStringAsync()).ShouldContain(ServiceErrorCodes.Disabled);
    }

    [Fact]
    public async Task Service_catalog_should_render_and_serve_real_page2_filter_sort()
    {
        Enable();
        var app = GetRequiredService<IServiceWorkAppService>();
        for (var i = 0; i < 12; i++)
            await app.CreateAsync(new() { Code = $"WEB-{i:D2}", Name = $"Work {i:D2}", Unit = "Visit" });
        var page = await Client.GetAsync("/Service/Works");
        page.StatusCode.ShouldBe(HttpStatusCode.OK, await page.Content.ReadAsStringAsync());
        var markup = await page.Content.ReadAsStringAsync();
        markup.ShouldContain("ServiceWorksTable");
        markup.ShouldContain("CreateServiceWork");
        var response = await Client.GetAsync("/Service/Works?handler=List&searchText=WEB&sorting=name%20desc&skipCount=10&maxResultCount=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ServiceWorkDto>>();
        result!.TotalCount.ShouldBe(12);
        result.Items.Count.ShouldBe(2);
        result.Items.First().Code.ShouldBe("WEB-01");
        result.Items.Last().Code.ShouldBe("WEB-00");
        result.Items.Last().StandardCost.ShouldBeNull();
        (await GetRequiredService<IMenuManager>().GetAsync(StandardMenus.Main)).Items.Any(x => x.Name == "VPureLux.Service").ShouldBeTrue();
    }

    [Fact]
    public async Task Service_modal_should_reject_missing_token_and_accept_valid_create_and_edit()
    {
        Enable();
        using var missing = await Client.PostAsync("/Service/WorkModal", new FormUrlEncodedContent(new Dictionary<string, string>()));
        missing.StatusCode.ShouldBe(HttpStatusCode.Found);
        missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        await SubmitModalAsync("/Service/WorkModal", new()
        {
            ["Input.Code"] = "WEB-FORM", ["Input.Name"] = "Labor", ["Input.Unit"] = "Visit",
            ["Input.DefaultPrice"] = "123.50", ["Input.StandardCost"] = "", ["Input.Status"] = "1"
        });
        var app = GetRequiredService<IServiceWorkAppService>();
        var work = (await app.GetListAsync(new() { SearchText = "WEB-FORM" })).Items.Single();
        work.StandardCost.ShouldBeNull();
        work.DefaultPrice.ShouldBe(123.50m);
        await SubmitModalAsync($"/Service/WorkModal?id={work.Id}", new()
        {
            ["Id"] = work.Id.ToString(), ["Input.Code"] = work.Code, ["Input.Name"] = "Labor edited",
            ["Input.Unit"] = "Hour", ["Input.DefaultPrice"] = "125.25", ["Input.StandardCost"] = "0",
            ["Input.Status"] = "2", ["Input.ConcurrencyStamp"] = work.ConcurrencyStamp
        });
        var edited = await app.GetAsync(work.Id);
        edited.StandardCost.ShouldBe(0);
        edited.Status.ShouldBe(ServiceWorkStatus.Inactive);
        edited.Unit.ShouldBe("Hour");
    }

    [Fact]
    public async Task Service_permission_should_deny_page_and_api_then_allow()
    {
        Enable();
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/app/service-work"))
        {
            request.Headers.Add("X-S001-Deny", VPureLuxPermissions.Service.View);
            var api = await Client.SendAsync(request);
            api.StatusCode.ShouldBe(HttpStatusCode.Found);
            api.Headers.Location!.ToString().ShouldContain("AccessDenied");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/Service/WorkModal"))
        {
            request.Headers.Add("X-S001-Deny", VPureLuxPermissions.Service.ManageWorks);
            var denied = await Client.SendAsync(request);
            denied.IsSuccessStatusCode.ShouldBeFalse();
        }
        (await Client.GetAsync("/api/app/service-work")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync("/Service/WorkModal")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task SubmitModalAsync(string url, Dictionary<string, string> input)
    {
        var get = await Client.GetAsync(url);
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        var html = new HtmlDocument();
        html.LoadHtml(await get.Content.ReadAsStringAsync());
        input["__RequestVerificationToken"] = html.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']").GetAttributeValue("value", "");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(input) };
        if (get.Headers.TryGetValues("Set-Cookie", out var cookies))
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(x => x.Split(';')[0])));
        var result = await Client.SendAsync(request);
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent, await result.Content.ReadAsStringAsync());
    }
}

public class ServiceWorkUiSourceTests
{
    [Fact]
    public void Work_ui_should_use_abp_modals_server_paging_and_honest_nullable_cost()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "VPureLux.slnx"))) directory = directory.Parent;
        directory.ShouldNotBeNull();
        var js = File.ReadAllText(Path.Combine(directory!.FullName, "src/VPureLux.Web/Pages/Service/Works.js"));
        js.ShouldContain("new abp.ModalManager");
        js.ShouldContain("serverSide: true");
        js.ShouldContain("createAjax");
        js.ShouldContain("Service:Unknown");
        js.ShouldContain("data.record || data");
        js.ShouldNotContain("prompt(");
        js.ShouldNotContain("alert(");
        js.ShouldNotContain("confirm(");
    }
}
