using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp;

namespace VPureLux.Inventory;

public class EfCoreInventoryTransactionRepository : EfCoreRepository<VPureLuxDbContext, InventoryTransaction, Guid>, IInventoryTransactionRepository
{
    public EfCoreInventoryTransactionRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<InventoryTransaction?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).Include(x => x.Lines).ThenInclude(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, GetCancellationToken(cancellationToken));

    public async Task<List<InventoryTransaction>> GetLedgerAsync(Guid? warehouseId = null, Guid? stockItemId = null, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).Include(x => x.Lines).ThenInclude(x => x.Allocations)
            .Where(x => x.Status == InventoryTransactionStatus.Posted &&
                        (!warehouseId.HasValue || x.WarehouseId == warehouseId) &&
                        (!stockItemId.HasValue || x.Lines.Any(line => line.StockItemId == stockItemId)))
            .OrderByDescending(x => x.PostedAt).ThenByDescending(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));

    public override async Task<InventoryTransaction?> FindAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default) =>
        includeDetails
            ? await (await GetDbSetAsync()).Include(x => x.Lines).ThenInclude(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == id, GetCancellationToken(cancellationToken))
            : await base.FindAsync(id, false, cancellationToken);

    public override async Task<InventoryTransaction> InsertAsync(InventoryTransaction entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.InsertAsync(entity, autoSave, cancellationToken);
        }
        catch (DbUpdateException exception) when (ContainsIdempotencyIndex(exception))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict)
                .WithData(nameof(entity.IdempotencyKey), entity.IdempotencyKey);
        }
    }

    private static bool ContainsIdempotencyIndex(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("UX_InventoryTransactions_IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("AppInventoryTransactions.IdempotencyKey", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
