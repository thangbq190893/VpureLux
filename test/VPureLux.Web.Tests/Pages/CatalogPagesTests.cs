using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Localization;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Component_Page_Should_Render_Permitted_Actions()
    {
        var response = await GetResponseAsStringAsync("/Catalog/Components");

        response.ShouldContain("/Catalog/Components/Create");
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
    }

    [Fact]
    public async Task Product_Page_Should_Render_Permitted_Actions()
    {
        var response = await GetResponseAsStringAsync("/Catalog/Products");

        response.ShouldContain("/Catalog/Products/Create");
    }

    [Fact]
    public async Task Catalog_Api_Should_Use_Documented_Route_And_Response_Wrapper()
    {
        var response = await GetResponseAsStringAsync("/api/catalog/components?page=1&pageSize=10");

        response.ShouldContain("\"success\":true");
        response.ShouldContain("\"data\":");
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
