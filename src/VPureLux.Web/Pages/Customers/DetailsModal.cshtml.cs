using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Customers;

[Authorize(VPureLuxPermissions.Customers.View)]
public class DetailsModalModel : VPureLuxPageModel
{
    private readonly ICustomerAppService _customerAppService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public CustomerDto Customer { get; private set; } = new();

    public DetailsModalModel(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public async Task OnGetAsync()
    {
        Customer = await _customerAppService.GetAsync(Id);
    }
}
