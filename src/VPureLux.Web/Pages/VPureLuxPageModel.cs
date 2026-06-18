using VPureLux.Localization;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace VPureLux.Web.Pages;

public abstract class VPureLuxPageModel : AbpPageModel
{
    protected VPureLuxPageModel()
    {
        LocalizationResourceType = typeof(VPureLuxResource);
    }

    protected void AddBusinessError(BusinessException exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Code)
            ? exception.Message
            : L[exception.Code].Value;
        ModelState.AddModelError(string.Empty, message);
    }
}
