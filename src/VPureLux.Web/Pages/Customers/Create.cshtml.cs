using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Customers;

[Authorize(VPureLuxPermissions.Customers.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly ICustomerAppService _customerAppService;
    private readonly ICustomerGroupAppService _customerGroupAppService;
    [BindProperty] public CreateCustomerDto Input { get; set; } = new();
    public List<SelectListItem> CustomerGroups { get; private set; } = new();

    public CreateModel(ICustomerAppService customerAppService, ICustomerGroupAppService customerGroupAppService)
    {
        _customerAppService = customerAppService;
        _customerGroupAppService = customerGroupAppService;
    }

    public Task OnGetAsync() => LoadGroupsAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        await _customerAppService.CreateAsync(Input);
        return RedirectToPage("/Customers/Index");
    }
    private async Task LoadGroupsAsync()
    {
        var groups = await _customerGroupAppService.GetListAsync(new GetCustomerGroupListInput { Status = CustomerGroupStatus.Active, MaxResultCount = 100 });
        CustomerGroups = groups.Items
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
