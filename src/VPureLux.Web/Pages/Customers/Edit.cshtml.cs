using System;
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

[Authorize(VPureLuxPermissions.Customers.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly ICustomerAppService _customerAppService;
    private readonly ICustomerGroupAppService _customerGroupAppService;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public string Code { get; private set; } = string.Empty;
    [BindProperty] public UpdateCustomerDto Input { get; set; } = new();
    public List<SelectListItem> CustomerGroups { get; private set; } = new();
    public EditModel(ICustomerAppService customerAppService, ICustomerGroupAppService customerGroupAppService) { _customerAppService = customerAppService; _customerGroupAppService = customerGroupAppService; }
    public async Task OnGetAsync()
    {
        var customer = await _customerAppService.GetAsync(Id);
        Code = customer.Code;
        Input = new UpdateCustomerDto { Name = customer.Name, CustomerGroupId = customer.CustomerGroupId, PhoneNumber = customer.PhoneNumber, Email = customer.Email, Address = customer.Address, TaxCode = customer.TaxCode, Notes = customer.Notes };
        await LoadGroupsAsync(customer.CustomerGroupId);
    }
    public async Task<IActionResult> OnPostAsync() { await _customerAppService.UpdateAsync(Id, Input); return RedirectToPage("/Customers/Index"); }
    private async Task LoadGroupsAsync(Guid selectedId)
    {
        var groups = (await _customerGroupAppService.GetListAsync(new GetCustomerGroupListInput { MaxResultCount = 100 })).Items.Where(x => x.Status == CustomerGroupStatus.Active || x.Id == selectedId);
        CustomerGroups = groups.Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == selectedId)).ToList();
    }
}
