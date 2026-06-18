using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using Xunit;

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
        var service = Substitute.For<VPureLux.Sales.ISalesOrderAppService>();
        service.GetListAsync(Arg.Any<VPureLux.Sales.GetSalesOrderListInput>())
            .Returns(new Volo.Abp.Application.Dtos.PagedResultDto<VPureLux.Sales.SalesOrderDto>());
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        var model = new Web.Pages.Sales.IndexModel(service, authorization)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } }
        };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanViewHistory.ShouldBeFalse();
    }
}
