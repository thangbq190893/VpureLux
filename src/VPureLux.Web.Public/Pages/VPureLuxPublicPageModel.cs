using VPureLux.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace VPureLux.Web.Public.Pages;

/* Inherit your Page Model classes from this class.
 */
public abstract class VPureLuxPublicPageModel : AbpPageModel
{
    protected VPureLuxPublicPageModel()
    {
        LocalizationResourceType = typeof(VPureLuxResource);
    }
}
