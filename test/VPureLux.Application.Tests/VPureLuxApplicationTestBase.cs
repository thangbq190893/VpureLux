using Volo.Abp.Modularity;

namespace VPureLux;

public abstract class VPureLuxApplicationTestBase<TStartupModule> : VPureLuxTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
