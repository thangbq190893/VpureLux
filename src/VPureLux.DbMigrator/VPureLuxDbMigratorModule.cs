using VPureLux.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace VPureLux.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(VPureLuxEntityFrameworkCoreModule),
    typeof(VPureLuxApplicationContractsModule)
)]
public class VPureLuxDbMigratorModule : AbpModule
{
}
