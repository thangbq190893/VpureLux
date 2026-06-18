using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using System.Threading.Tasks;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IAuthorizationService _authorizationService;
    public bool CanReceive { get; private set; }
    public bool CanIssue { get; private set; }
    public bool CanAdjust { get; private set; }
    public bool CanManageWarehouses { get; private set; }
    public bool CanViewLedger { get; private set; }

    public IndexModel(IAuthorizationService authorizationService) => _authorizationService = authorizationService;

    public async Task OnGetAsync()
    {
        CanReceive = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Receive)).Succeeded;
        CanIssue = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Issue)).Succeeded;
        CanAdjust = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Adjust)).Succeeded;
        CanManageWarehouses = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.ManageWarehouses)).Succeeded;
        CanViewLedger = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.ViewLedger)).Succeeded;
    }
}
