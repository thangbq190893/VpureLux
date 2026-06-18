using VPureLux.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class VPureLuxController : AbpControllerBase
{
    protected VPureLuxController()
    {
        LocalizationResource = typeof(VPureLuxResource);
    }
}
