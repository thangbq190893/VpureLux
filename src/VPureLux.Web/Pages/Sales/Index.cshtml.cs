using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    public PagedResultDto<SalesOrderDto> Orders { get; private set; } = new();
    public bool CanCreate { get; private set; }
    public bool CanViewHistory { get; private set; }

    public IndexModel(ISalesOrderAppService service, IAuthorizationService authorizationService)
    {
        _service = service;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        Orders = await _service.GetListAsync(new GetSalesOrderListInput { MaxResultCount = 100 });
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Create)).Succeeded;
        CanViewHistory = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.ViewCustomerHistory)).Succeeded;
    }
}
