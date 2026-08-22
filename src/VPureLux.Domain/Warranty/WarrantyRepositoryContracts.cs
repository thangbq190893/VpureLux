using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Warranty;

public interface IComponentReplacementPolicyRepository : IRepository<ComponentReplacementPolicy, Guid>
{
    Task<ComponentReplacementPolicy?> FindByComponentIdAsync(Guid componentId, CancellationToken cancellationToken = default);

    Task<List<ComponentReplacementPolicy>> GetEnabledByComponentIdsAsync(
        IReadOnlyCollection<Guid> componentIds,
        CancellationToken cancellationToken = default);
}

public interface IWarrantyReadRepository
{
    Task<long> GetPolicyCountAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default);

    Task<List<WarrantyPolicyListItem>> GetPolicyListAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetReminderCountAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default);

    Task<List<WarrantyReminderListItem>> GetReminderListAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default);
}
