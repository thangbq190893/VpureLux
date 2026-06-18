using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Data;

/* This is used if database provider does't define
 * IVPureLuxDbSchemaMigrator implementation.
 */
public class NullVPureLuxDbSchemaMigrator : IVPureLuxDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
