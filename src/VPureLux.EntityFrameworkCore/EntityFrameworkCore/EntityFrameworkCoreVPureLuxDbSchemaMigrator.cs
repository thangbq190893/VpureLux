using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VPureLux.Data;
using Volo.Abp.DependencyInjection;

namespace VPureLux.EntityFrameworkCore;

public class EntityFrameworkCoreVPureLuxDbSchemaMigrator
    : IVPureLuxDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreVPureLuxDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the VPureLuxDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<VPureLuxDbContext>()
            .Database
            .MigrateAsync();
    }
}
