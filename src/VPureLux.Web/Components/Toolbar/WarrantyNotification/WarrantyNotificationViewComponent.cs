using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Warranty;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Web.Components.Toolbar.WarrantyNotification;

public class WarrantyNotificationViewComponent : AbpViewComponent
{
    private readonly IWarrantyAppService _warrantyAppService;

    public WarrantyNotificationViewComponent(IWarrantyAppService warrantyAppService)
    {
        _warrantyAppService = warrantyAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var summary = await _warrantyAppService.GetNotificationSummaryAsync();
        return View("~/Components/Toolbar/WarrantyNotification/Default.cshtml", summary);
    }
}
