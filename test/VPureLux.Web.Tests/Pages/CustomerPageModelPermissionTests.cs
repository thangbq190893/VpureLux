using System.Reflection;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;
using Xunit;

namespace VPureLux.Pages;

public class CustomerPageModelPermissionTests
{
    [Fact]
    public async Task Customer_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var service = Substitute.For<ICustomerAppService>();
        var authorization = DeniedAuthorization();
        service.GetListAsync(Arg.Any<GetCustomerListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerDto>());
        var model = new Web.Pages.Customers.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeFalse();
    }

    [Fact]
    public async Task Customer_Actions_Should_Follow_Exact_Permissions()
    {
        var service = Substitute.For<ICustomerAppService>();
        service.GetListAsync(Arg.Any<GetCustomerListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerDto>());
        var authorization = AuthorizationFor(
            VPureLuxPermissions.Customers.Create,
            VPureLuxPermissions.Customers.Edit,
            VPureLuxPermissions.Customers.ManageStatus);
        var model = new Web.Pages.Customers.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeTrue();
        model.CanEdit.ShouldBeTrue();
        model.CanManageStatus.ShouldBeTrue();
    }

    [Fact]
    public async Task Customer_Status_Action_Should_Require_ManageStatus_Permission()
    {
        var service = Substitute.For<ICustomerAppService>();
        service.GetListAsync(Arg.Any<GetCustomerListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerDto>());
        var authorization = AuthorizationFor(VPureLuxPermissions.Customers.View);
        var model = new Web.Pages.Customers.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeFalse();

        authorization = AuthorizationFor(VPureLuxPermissions.Customers.ManageStatus);
        model = new Web.Pages.Customers.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeTrue();
    }

    [Fact]
    public async Task CustomerGroup_Actions_Should_Be_Hidden_Without_Permissions()
    {
        var service = Substitute.For<ICustomerGroupAppService>();
        var authorization = DeniedAuthorization();
        service.GetListAsync(Arg.Any<GetCustomerGroupListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerGroupDto>());
        var model = new Web.Pages.CustomerGroups.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeFalse();
    }

    [Fact]
    public async Task CustomerGroup_Actions_Should_Follow_Exact_Permissions()
    {
        var service = Substitute.For<ICustomerGroupAppService>();
        service.GetListAsync(Arg.Any<GetCustomerGroupListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerGroupDto>());
        var authorization = AuthorizationFor(
            VPureLuxPermissions.CustomerGroups.Create,
            VPureLuxPermissions.CustomerGroups.Edit,
            VPureLuxPermissions.CustomerGroups.ManageStatus);
        var model = new Web.Pages.CustomerGroups.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeTrue();
        model.CanEdit.ShouldBeTrue();
        model.CanManageStatus.ShouldBeTrue();
    }

    [Fact]
    public async Task CustomerGroup_Status_Action_Should_Require_ManageStatus_Permission()
    {
        var service = Substitute.For<ICustomerGroupAppService>();
        service.GetListAsync(Arg.Any<GetCustomerGroupListInput>()).Returns(new Volo.Abp.Application.Dtos.PagedResultDto<CustomerGroupDto>());
        var authorization = AuthorizationFor(VPureLuxPermissions.CustomerGroups.View);
        var model = new Web.Pages.CustomerGroups.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeFalse();

        authorization = AuthorizationFor(VPureLuxPermissions.CustomerGroups.ManageStatus);
        model = new Web.Pages.CustomerGroups.IndexModel(service, authorization) { PageContext = CreatePageContext() };

        await model.OnGetAsync();

        model.CanCreate.ShouldBeFalse();
        model.CanEdit.ShouldBeFalse();
        model.CanManageStatus.ShouldBeTrue();
    }

    [Fact]
    public void Customer_PageModels_Should_Use_Exact_Authorization_Policies()
    {
        typeof(Web.Pages.Customers.IndexModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Customers.View);
        typeof(Web.Pages.Customers.CreateModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Customers.Create);
        typeof(Web.Pages.Customers.EditModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Customers.Edit);
        typeof(Web.Pages.Customers.DetailsModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.Customers.View);
    }

    [Fact]
    public void CustomerGroup_PageModels_Should_Use_Exact_Authorization_Policies()
    {
        typeof(Web.Pages.CustomerGroups.IndexModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.CustomerGroups.View);
        typeof(Web.Pages.CustomerGroups.CreateModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.CustomerGroups.Create);
        typeof(Web.Pages.CustomerGroups.EditModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.CustomerGroups.Edit);
        typeof(Web.Pages.CustomerGroups.DetailsModel).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(VPureLuxPermissions.CustomerGroups.View);
    }

    private static IAuthorizationService DeniedAuthorization()
    {
        var service = Substitute.For<IAuthorizationService>();
        service.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        return service;
    }

    private static IAuthorizationService AuthorizationFor(params string[] grantedPolicies)
    {
        var service = Substitute.For<IAuthorizationService>();
        service.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(call =>
            {
                var policy = call.ArgAt<string>(2);
                return grantedPolicies.Contains(policy)
                    ? AuthorizationResult.Success()
                    : AuthorizationResult.Failed();
            });
        return service;
    }

    private static PageContext CreatePageContext() => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
    };
}
