using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
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
            : LocalizeErrorCode(exception.Code);
        ModelState.AddModelError(string.Empty, message);
    }

    private string LocalizeErrorCode(string code)
    {
        var localizer = HttpContext?.RequestServices.GetService<IStringLocalizer<VPureLuxResource>>();
        var localized = localizer?[code];

        if (localized is { ResourceNotFound: false })
        {
            return localized.Value;
        }

        return L[code].Value;
    }
}
