using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Inventory;

public class EfCoreStockItemRepository : EfCoreRepository<VPureLuxDbContext, StockItem, Guid>, IStockItemRepository
{
    public EfCoreStockItemRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<StockItem?> FindByCatalogItemAsync(StockItemType itemType, Guid catalogItemId, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ItemType == itemType && x.CatalogItemId == catalogItemId,
            GetCancellationToken(cancellationToken));
}
