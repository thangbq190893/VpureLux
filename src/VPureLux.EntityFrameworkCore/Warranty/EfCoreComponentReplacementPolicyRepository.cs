using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Warranty;

public class EfCoreComponentReplacementPolicyRepository :
    EfCoreRepository<VPureLuxDbContext, ComponentReplacementPolicy, Guid>,
    IComponentReplacementPolicyRepository
{
    public EfCoreComponentReplacementPolicyRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<ComponentReplacementPolicy?> FindByComponentIdAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .FirstOrDefaultAsync(x => x.ComponentId == componentId, GetCancellationToken(cancellationToken));
    }

    public async Task<List<ComponentReplacementPolicy>> GetEnabledByComponentIdsAsync(
        IReadOnlyCollection<Guid> componentIds,
        CancellationToken cancellationToken = default)
    {
        if (componentIds.Count == 0)
        {
            return new List<ComponentReplacementPolicy>();
        }

        return await (await GetDbSetAsync())
            .Where(x => x.IsEnabled && componentIds.Contains(x.ComponentId))
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}
