using Volo.Abp.Settings;

namespace VPureLux.Settings;

public class VPureLuxSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(VPureLuxSettings.MySetting1));
    }
}
