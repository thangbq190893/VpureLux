using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Catalog;

public class EfCoreComponentRepository :
    EfCoreRepository<VPureLuxDbContext, Component, Guid>,
    IComponentRepository
{
    public EfCoreComponentRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<Component?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .FirstOrDefaultAsync(x => x.Code == code, GetCancellationToken(cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .AnyAsync(
                x => x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value),
                GetCancellationToken(cancellationToken));
    }

    public async Task<int> GetMaxCodeSequenceAsync(string codePrefix, CancellationToken cancellationToken = default)
    {
        var codes = await (await GetDbSetAsync())
            .Where(x => x.Code.StartsWith(codePrefix))
            .Select(x => x.Code)
            .ToListAsync(GetCancellationToken(cancellationToken));

        return codes
            .Select(code => TryParseSequence(code, codePrefix, out var sequence) ? sequence : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static bool TryParseSequence(string code, string codePrefix, out int sequence)
    {
        sequence = 0;
        return code.Length > codePrefix.Length &&
               int.TryParse(code[codePrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out sequence);
    }
}
