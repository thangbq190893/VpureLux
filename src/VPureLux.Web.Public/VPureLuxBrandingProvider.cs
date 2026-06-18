using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;

namespace VPureLux.Web.Public;

[Dependency(ReplaceServices = true)]
public class VPureLuxBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<VPureLuxResource> _localizer;

    public VPureLuxBrandingProvider(IStringLocalizer<VPureLuxResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
