using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class HistoryModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    public PagedResultDto<SalesOrderDto> Orders { get; private set; } = new();
    public HistoryModel(ISalesOrderAppService service) => _service = service;
    public async Task OnGetAsync() => Orders = await _service.GetListAsync(new GetSalesOrderListInput
    {
        Status = SalesOrderStatus.Confirmed,
        MaxResultCount = 500
    });
}
