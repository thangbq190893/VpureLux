using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Bom;

public interface IBomVersionRepository : IRepository<BomVersion, Guid>
{
    Task<int> GetNextVersionNoAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<bool> HasPublishedVersionAsync(
        Guid productId,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<List<BomVersion>> GetListByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
