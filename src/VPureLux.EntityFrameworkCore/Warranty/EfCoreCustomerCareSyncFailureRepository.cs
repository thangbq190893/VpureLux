using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Warranty;

public class EfCoreCustomerCareSyncFailureRepository :
    EfCoreRepository<VPureLuxDbContext, CustomerCareSyncFailure, Guid>,
    ICustomerCareSyncFailureRepository
{
    public EfCoreCustomerCareSyncFailureRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<CustomerCareSyncFailure?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.IdempotencyKey == idempotencyKey,
            GetCancellationToken(cancellationToken));
}
