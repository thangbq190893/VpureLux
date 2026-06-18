using VPureLux.Localization;
using Volo.Abp.Application.Services;

namespace VPureLux;

/* Inherit your application services from this class.
 */
public abstract class VPureLuxAppService : ApplicationService
{
    protected VPureLuxAppService()
    {
        LocalizationResource = typeof(VPureLuxResource);
    }
}
