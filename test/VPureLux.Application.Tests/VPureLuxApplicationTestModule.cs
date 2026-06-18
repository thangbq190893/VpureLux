using Volo.Abp.Modularity;

namespace VPureLux;

[DependsOn(
    typeof(VPureLuxApplicationModule),
    typeof(VPureLuxDomainTestModule)
)]
public class VPureLuxApplicationTestModule : AbpModule
{

}
