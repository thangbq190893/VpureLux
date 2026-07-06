using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPureLux.BusinessCodes;

public sealed record BusinessCodeGenerationContext
{
    public const int DefaultPaddingLength = 4;
    public const int DefaultRetryLimit = 20;

    public string SequenceName { get; init; } = string.Empty;

    public string Prefix { get; init; } = string.Empty;

    public DateTime? Date { get; init; }

    public Func<string, CancellationToken, Task<bool>> ExistsAsync { get; init; } = (_, _) => Task.FromResult(false);

    public Func<CancellationToken, Task<int?>> SeedMaxAsync { get; init; } = _ => Task.FromResult<int?>(0);

    public int PaddingLength { get; init; } = DefaultPaddingLength;

    public int RetryLimit { get; init; } = DefaultRetryLimit;
}
