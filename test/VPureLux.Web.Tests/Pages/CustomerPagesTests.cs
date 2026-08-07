using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Localization;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Customer_And_Group_Pages_Should_Render_Permitted_Actions_And_Navigation()
    {
        var customers = await GetResponseAsStringAsync("/Customers");
        customers.ShouldContain("/Customers/Create");
        customers.ShouldContain("id=\"CustomersTable\"");
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        WebUtility.HtmlDecode(customers).ShouldContain(localizer["Customers:Title"].Value);

        var groups = await GetResponseAsStringAsync("/CustomerGroups");
        groups.ShouldContain("/CustomerGroups/Create");
        groups.ShouldContain("id=\"CustomerGroupsTable\"");
        WebUtility.HtmlDecode(groups).ShouldContain(localizer["CustomerGroups:Title"].Value);
    }

    [Fact]
    public async Task Customer_And_Group_Routes_Should_Render_Current_Full_Pages()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("ROUTE-G"), Name = "Route Group" });
        var customer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("ROUTE-C"), Name = "Route Customer", CustomerGroupId = group.Id });

        await AssertLocalizedPageAsync("/Customers", localizer["Customers:Title"].Value);
        await AssertLocalizedPageAsync("/Customers/Create", localizer["Customers:Create"].Value);
        await AssertLocalizedPageAsync($"/Customers/Edit/{customer.Id}", localizer["Customers:Edit"].Value);
        await AssertLocalizedPageAsync($"/Customers/Details/{customer.Id}", localizer["Customers:Details"].Value);

        await AssertLocalizedPageAsync("/CustomerGroups", localizer["CustomerGroups:Title"].Value);
        await AssertLocalizedPageAsync("/CustomerGroups/Create", localizer["CustomerGroups:Create"].Value);
        await AssertLocalizedPageAsync($"/CustomerGroups/Edit/{group.Id}", localizer["CustomerGroups:Edit"].Value);
        await AssertLocalizedPageAsync($"/CustomerGroups/Details/{group.Id}", localizer["CustomerGroups:Details"].Value);
    }

    [Fact]
    public async Task Customer_Create_And_Edit_Pages_Should_Render_Active_Group_Selection()
    {
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("PAGE-G"), Name = "Page Group" });
        var customer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("PAGE-C"), Name = "Page Customer", CustomerGroupId = group.Id });

        (await GetResponseAsStringAsync("/Customers/Create")).ShouldContain("Page Group");
        var edit = await GetResponseAsStringAsync($"/Customers/Edit/{customer.Id}");
        edit.ShouldContain("Page Customer");
        edit.ShouldContain("Page Group");
    }

    [Fact]
    public async Task Customer_Edit_Pages_Should_Render_Code_As_Readonly_Display()
    {
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("LOCK-G"), Name = "Readonly Group" });
        var customer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("LOCK-C"), Name = "Readonly Customer", CustomerGroupId = group.Id });
        var customerGroup = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("LOCK-CG"), Name = "Readonly Customer Group" });

        var customerEdit = await GetResponseAsStringAsync($"/Customers/Edit/{customer.Id}");
        customerEdit.ShouldContain(customer.Code);
        customerEdit.ShouldContain("disabled");

        var groupEdit = await GetResponseAsStringAsync($"/CustomerGroups/Edit/{customerGroup.Id}");
        groupEdit.ShouldContain(customerGroup.Code);
        groupEdit.ShouldContain("disabled");
    }

    [Fact]
    public async Task Details_Pages_Should_Render_Created_Data()
    {
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("DETAIL-G"), Name = "Detail Group" });
        var customer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("DETAIL-C"), Name = "Detail Customer", CustomerGroupId = group.Id });

        (await GetResponseAsStringAsync($"/Customers/Details/{customer.Id}")).ShouldContain("Detail Customer");
        (await GetResponseAsStringAsync($"/CustomerGroups/Details/{group.Id}")).ShouldContain("Detail Group");
    }

    [Fact]
    public async Task Customer_And_Group_Pages_Should_Render_Localized_Action_Labels()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("LOC-G"), Name = "Localized Group" });
        var customer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("LOC-C"), Name = "Localized Customer", CustomerGroupId = group.Id });

        var customers = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Customers"));
        customers.ShouldContain(localizer["Customers:Create"].Value);
        customers.ShouldContain("id=\"CustomersTable\"");

        var customerCreate = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Customers/Create"));
        customerCreate.ShouldContain(localizer["Save"].Value);
        customerCreate.ShouldContain(localizer["Cancel"].Value);

        var customerDetails = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Customers/Details/{customer.Id}"));
        customerDetails.ShouldContain(localizer["Back"].Value);

        var groups = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/CustomerGroups"));
        groups.ShouldContain(localizer["CustomerGroups:Create"].Value);
        groups.ShouldContain("id=\"CustomerGroupsTable\"");

        var groupCreate = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/CustomerGroups/Create"));
        groupCreate.ShouldContain(localizer["Save"].Value);
        groupCreate.ShouldContain(localizer["Cancel"].Value);

        var groupDetails = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/CustomerGroups/Details/{group.Id}"));
        groupDetails.ShouldContain(localizer["Back"].Value);
    }

    [Fact]
    public async Task CustomerGroup_Index_Should_Render_Status_Confirmation_Hooks_And_Page_Script()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var activeGroup = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("HOOK-A"), Name = "Active Hook Group" });
        var inactiveGroup = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("HOOK-I"), Name = "Inactive Hook Group" });
        await groupService.DeactivateAsync(inactiveGroup.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/CustomerGroups"));

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/CustomerGroups/Index.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/CustomerGroups/Index.js\" />");

        html.ShouldContain("data-customer-groups-index");
        html.ShouldContain("id=\"CustomerGroupsTable\"");
        html.ShouldContain("data-customer-group-create");
        html.ShouldContain("data-can-edit=\"true\"");
        html.ShouldContain("data-can-manage-status=\"true\"");
        html.ShouldNotContain(activeGroup.Name);
        html.ShouldNotContain(inactiveGroup.Name);

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/CustomerGroups/Index.js"));
        scriptSource.ShouldContain("rowAction");
        scriptSource.ShouldContain("CustomerGroups:ConfirmDeactivate");
        scriptSource.ShouldContain("CustomerGroups:ConfirmActivate");
        scriptSource.ShouldContain("dataTable.ajax.reload(null, false)");
    }

    [Fact]
    public async Task Customer_Index_Should_Render_Status_Confirmation_Hooks_And_Page_Script()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var groupService = GetRequiredService<ICustomerGroupAppService>();
        var customerService = GetRequiredService<ICustomerAppService>();
        var group = await groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("CH-G"), Name = "Customer Hook Group" });
        var activeCustomer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("CH-A"), Name = "Active Hook Customer", CustomerGroupId = group.Id });
        var inactiveCustomer = await customerService.CreateAsync(new CreateCustomerDto { Code = Unique("CH-I"), Name = "Inactive Hook Customer", CustomerGroupId = group.Id });
        await customerService.DeactivateAsync(inactiveCustomer.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Customers"));

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Customers/Index.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Customers/Index.js\" />");

        html.ShouldContain("data-customers-index");
        html.ShouldContain("id=\"CustomersTable\"");
        html.ShouldContain("data-customer-create");
        html.ShouldContain("data-can-edit=\"true\"");
        html.ShouldContain("data-can-manage-status=\"true\"");
        html.ShouldNotContain(activeCustomer.Name);
        html.ShouldNotContain(inactiveCustomer.Name);

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Customers/Index.js"));
        scriptSource.ShouldContain("rowAction");
        scriptSource.ShouldContain("Customers:ConfirmDeactivate");
        scriptSource.ShouldContain("Customers:ConfirmActivate");
        scriptSource.ShouldContain("dataTable.ajax.reload(null, false)");
    }

    [Fact]
    public async Task Customer_Create_Pages_Should_Show_Auto_Code_Hint_And_Not_Post_Code_Input()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();

        foreach (var route in new[] { "/Customers/Create", "/Customers/CreateModal" })
        {
            var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(route));

            html.ShouldContain(localizer["Customers:CodeAutoGeneratedOnSave"].Value);
            html.ShouldNotContain("name=\"Input.Code\"");
        }
    }

    [Fact]
    public async Task Customer_And_Group_Index_Should_Use_Abp_DataTables_Server_Paging()
    {
        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Customers/Index.cshtml",
            "src/VPureLux.Web/Pages/CustomerGroups/Index.cshtml"
        })
        {
            var pageSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageSource.ShouldContain("<abp-table id=");
            pageSource.ShouldNotContain("foreach (var customer in Model.Customers)");
            pageSource.ShouldNotContain("foreach (var group in Model.CustomerGroups)");
        }

        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Customers/Index.cshtml.cs",
            "src/VPureLux.Web/Pages/CustomerGroups/Index.cshtml.cs"
        })
        {
            var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            pageModelSource.ShouldContain("DefaultSorting = \"CreationTime DESC\"");
            pageModelSource.ShouldContain("OnGetListAsync");
            pageModelSource.ShouldNotContain("MaxResultCount = 100");
        }

        foreach (var relativePath in new[]
        {
            "src/VPureLux.Web/Pages/Customers/Index.js",
            "src/VPureLux.Web/Pages/CustomerGroups/Index.js"
        })
        {
            var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath(relativePath));
            scriptSource.ShouldContain("DataTable");
            scriptSource.ShouldContain("serverSide: true");
            scriptSource.ShouldContain("abp.libs.datatables.createAjax");
            scriptSource.ShouldContain("handler=List");
            scriptSource.ShouldContain("event.preventDefault()");
            scriptSource.ShouldContain("ClearButton");
            scriptSource.ShouldContain("function recordOf(data)");
            scriptSource.IndexOf("select2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        }
    }

    private async Task AssertLocalizedPageAsync(string route, string expectedText)
    {
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(route));
        html.ShouldContain(expectedText);
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

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
