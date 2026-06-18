using Volo.Abp.Modularity;

namespace VPureLux;

[DependsOn(
    typeof(VPureLuxDomainModule),
    typeof(VPureLuxTestBaseModule)
)]
public class VPureLuxDomainTestModule : AbpModule
{

}
