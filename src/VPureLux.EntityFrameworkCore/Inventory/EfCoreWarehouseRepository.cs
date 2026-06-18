using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Inventory;

public class EfCoreWarehouseRepository : EfCoreRepository<VPureLuxDbContext, Warehouse, Guid>, IWarehouseRepository
{
    public EfCoreWarehouseRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }
    public async Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).AnyAsync(x => x.Code == code && (!excludedId.HasValue || x.Id != excludedId), GetCancellationToken(cancellationToken));
}
