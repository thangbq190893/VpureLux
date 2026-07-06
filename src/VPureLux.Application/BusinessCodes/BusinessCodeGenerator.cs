using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Timing;

namespace VPureLux.BusinessCodes;

public sealed class BusinessCodeGenerator : IBusinessCodeGenerator, ITransientDependency
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromDays(7);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);

    private readonly IDistributedCache _cache;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IClock _clock;

    public BusinessCodeGenerator(
        IDistributedCache cache,
        IAbpDistributedLock distributedLock,
        IClock clock)
    {
        _cache = cache;
        _distributedLock = distributedLock;
        _clock = clock;
    }

    public async Task<string> GenerateAsync(
        BusinessCodeGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(context, nameof(context));

        var sequenceName = NormalizeToken(context.SequenceName, nameof(context.SequenceName));
        var prefix = NormalizeToken(context.Prefix, nameof(context.Prefix));
        var businessDate = (context.Date ?? _clock.Now).Date;
        var datePart = businessDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var paddingLength = context.PaddingLength > 0
            ? context.PaddingLength
            : BusinessCodeGenerationContext.DefaultPaddingLength;
        var retryLimit = context.RetryLimit > 0
            ? context.RetryLimit
            : BusinessCodeGenerationContext.DefaultRetryLimit;
        var cacheKey = $"Sequence:{sequenceName}:{datePart}";
        var lockKey = $"VPureLux:SequenceLock:{sequenceName}:{datePart}";

        await using var lockHandle = await _distributedLock.TryAcquireAsync(lockKey, LockTimeout, cancellationToken);
        if (lockHandle is null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.BusinessCodeGenerationUnavailable);
        }

        var latest = await GetLatestSequenceAsync(cacheKey, cancellationToken)
            ?? Math.Max(await GetSeedMaxAsync(context, cancellationToken), 0);

        for (var attempt = 0; attempt < retryLimit; attempt++)
        {
            latest++;
            var candidate = Format(prefix, datePart, latest, paddingLength);

            if (!await context.ExistsAsync(candidate, cancellationToken))
            {
                await SetLatestSequenceAsync(cacheKey, latest, cancellationToken);
                return candidate;
            }
        }

        await SetLatestSequenceAsync(cacheKey, latest, cancellationToken);
        throw new BusinessException(VPureLuxDomainErrorCodes.BusinessCodeGenerationRetryLimitExceeded)
            .WithData("SequenceName", sequenceName)
            .WithData("Prefix", prefix)
            .WithData("Date", datePart)
            .WithData("RetryLimit", retryLimit);
    }

    private async Task<int?> GetLatestSequenceAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        return int.TryParse(cached, NumberStyles.None, CultureInfo.InvariantCulture, out var latest)
            ? latest
            : null;
    }

    private static async Task<int> GetSeedMaxAsync(
        BusinessCodeGenerationContext context,
        CancellationToken cancellationToken)
    {
        var seed = await context.SeedMaxAsync(cancellationToken);
        return seed.GetValueOrDefault();
    }

    private Task SetLatestSequenceAsync(string cacheKey, int latest, CancellationToken cancellationToken) =>
        _cache.SetStringAsync(
            cacheKey,
            latest.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            },
            cancellationToken);

    private static string Format(string prefix, string datePart, int sequence, int paddingLength) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}-{datePart}{sequence.ToString($"D{paddingLength}", CultureInfo.InvariantCulture)}");

    private static string NormalizeToken(string value, string parameterName)
    {
        Check.NotNullOrWhiteSpace(value, parameterName);
        return value.Trim().ToUpperInvariant();
    }
}
