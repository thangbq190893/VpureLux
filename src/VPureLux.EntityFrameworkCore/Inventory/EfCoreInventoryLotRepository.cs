using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Inventory;

public class EfCoreInventoryLotRepository : EfCoreRepository<VPureLuxDbContext, InventoryLot, Guid>, IInventoryLotRepository
{
    public EfCoreInventoryLotRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<bool> LotNoExistsAsync(string lotNo, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync())
            .AnyAsync(x => x.LotNo == lotNo, GetCancellationToken(cancellationToken));

    public async Task<int> GetMaxLotNoSequenceAsync(string lotNoPrefix, CancellationToken cancellationToken = default)
    {
        var lotNos = await (await GetDbSetAsync())
            .Where(x => x.LotNo.StartsWith(lotNoPrefix))
            .Select(x => x.LotNo)
            .ToListAsync(GetCancellationToken(cancellationToken));

        return lotNos
            .Select(lotNo => TryParseSequence(lotNo, lotNoPrefix, out var sequence) ? sequence : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    public async Task<List<InventoryLot>> GetAvailableFifoLotsAsync(Guid warehouseId, Guid stockItemId, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).Where(x => x.WarehouseId == warehouseId && x.StockItemId == stockItemId && x.AvailableQuantity > 0)
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.CreationTime).ThenBy(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));

    public async Task<List<InventoryLot>> GetListAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null,
        string? lotNo = null,
        CancellationToken cancellationToken = default)
    {
        var query = await GetDbSetAsync();
        var normalizedLotNo = lotNo?.Trim();
        return await query.Where(x =>
                (!warehouseId.HasValue || x.WarehouseId == warehouseId) &&
                (!stockItemId.HasValue || x.StockItemId == stockItemId) &&
                (string.IsNullOrWhiteSpace(normalizedLotNo) || x.LotNo.Contains(normalizedLotNo)))
            .OrderByDescending(x => x.ReceivedAt).ThenByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<List<InventoryLot>> GetListByReceiptLineIdsAsync(
        IReadOnlyCollection<Guid> receiptTransactionLineIds,
        CancellationToken cancellationToken = default)
    {
        if (receiptTransactionLineIds.Count == 0)
        {
            return new List<InventoryLot>();
        }

        return await (await GetDbSetAsync())
            .Where(x => receiptTransactionLineIds.Contains(x.ReceiptTransactionLineId))
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.CreationTime).ThenBy(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    private static bool TryParseSequence(string lotNo, string lotNoPrefix, out int sequence)
    {
        sequence = 0;
        return lotNo.Length > lotNoPrefix.Length &&
               int.TryParse(lotNo[lotNoPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out sequence);
    }
}
