using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;

namespace VPureLux.Pages;

public class CatalogPageModelPermissionTests
{
    [Fact]
    public async Task Component_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var appService = Substitute.For<IComponentAppService>();
        var authorizationService = Substitute.For<IAuthorizationService>();
        appService.GetListAsync(Arg.Any<GetComponentListInput>())
            .Returns(new Volo.Abp.Application.Dtos.PagedResultDto<ComponentDto>());
        authorizationService.AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        var model = new Web.Pages.Catalog.Components.IndexModel(appService, authorizationService)
        {
            PageContext = CreatePageContext()
        };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Product_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var appService = Substitute.For<IProductAppService>();
        var authorizationService = Substitute.For<IAuthorizationService>();
        appService.GetListAsync(Arg.Any<GetProductListInput>())
            .Returns(new Volo.Abp.Application.Dtos.PagedResultDto<ProductDto>());
        authorizationService.AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        var model = new Web.Pages.Catalog.Products.IndexModel(appService, authorizationService)
        {
            PageContext = CreatePageContext()
        };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
    }

    private static PageContext CreatePageContext()
    {
        return new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
    }
}
