using Volo.Abp.Modularity;

namespace VPureLux;

/* Inherit from this class for your domain layer tests. */
public abstract class VPureLuxDomainTestBase<TStartupModule> : VPureLuxTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
