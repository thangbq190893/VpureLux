using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Timing;
using Xunit;

namespace VPureLux.BusinessCodes;

public class BusinessCodeGeneratorTests
{
    private static readonly DateTime BusinessDate = new(2026, 7, 6);

    private readonly BusinessCodeGenerator _generator;
    private readonly TestDistributedCache _cache = new();

    public BusinessCodeGeneratorTests()
    {
        _generator = new BusinessCodeGenerator(
            _cache,
            new InMemoryAbpDistributedLock(),
            new TestClock(BusinessDate));
    }

    [Fact]
    public async Task Should_Generate_Daily_Business_Code_Format()
    {
        var code = await _generator.GenerateAsync(Context("Product", "prod"));

        code.ShouldBe("PROD-202607060001");
    }

    [Fact]
    public async Task Should_Increment_From_Cache_And_Seed_Only_Once()
    {
        var seedCalls = 0;
        var context = Context("Product", "PROD") with
        {
            SeedMaxAsync = _ =>
            {
                seedCalls++;
                return Task.FromResult<int?>(0);
            }
        };

        var first = await _generator.GenerateAsync(context);
        var second = await _generator.GenerateAsync(context);

        first.ShouldBe("PROD-202607060001");
        second.ShouldBe("PROD-202607060002");
        seedCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Seed_From_Db_Max_Suffix_When_Cache_Is_Missing()
    {
        var code = await _generator.GenerateAsync(Context("Product", "PROD") with
        {
            SeedMaxAsync = _ => Task.FromResult<int?>(41)
        });

        code.ShouldBe("PROD-202607060042");
    }

    [Fact]
    public async Task Should_Use_Max_Suffix_Not_Count()
    {
        var existingCodes = new HashSet<string>
        {
            "PROD-202607060001",
            "PROD-202607060003"
        };

        var code = await _generator.GenerateAsync(Context("Product", "PROD") with
        {
            SeedMaxAsync = _ => Task.FromResult<int?>(existingCodes
                .Select(x => int.Parse(x[^4..]))
                .Max())
        });

        code.ShouldBe("PROD-202607060004");
    }

    [Fact]
    public async Task Should_Retry_When_Candidate_Already_Exists()
    {
        var code = await _generator.GenerateAsync(Context("Product", "PROD") with
        {
            ExistsAsync = (candidate, _) => Task.FromResult(candidate == "PROD-202607060001")
        });

        code.ShouldBe("PROD-202607060002");
    }

    [Fact]
    public async Task Should_Throw_Friendly_Business_Exception_When_Retry_Limit_Is_Exhausted()
    {
        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _generator.GenerateAsync(Context("Product", "PROD") with
            {
                ExistsAsync = (_, _) => Task.FromResult(true),
                RetryLimit = 2
            }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.BusinessCodeGenerationRetryLimitExceeded);
    }

    [Fact]
    public async Task Should_Keep_Independent_Sequences()
    {
        var product = await _generator.GenerateAsync(Context("Product", "PROD"));
        var material = await _generator.GenerateAsync(Context("Material", "MAT"));
        var secondProduct = await _generator.GenerateAsync(Context("Product", "PROD"));

        product.ShouldBe("PROD-202607060001");
        material.ShouldBe("MAT-202607060001");
        secondProduct.ShouldBe("PROD-202607060002");
    }

    [Fact]
    public async Task Should_Reset_By_Date()
    {
        var first = await _generator.GenerateAsync(Context("Product", "PROD"));
        var nextDay = await _generator.GenerateAsync(Context("Product", "PROD") with
        {
            Date = BusinessDate.AddDays(1)
        });

        first.ShouldBe("PROD-202607060001");
        nextDay.ShouldBe("PROD-202607070001");
    }

    [Fact]
    public async Task Should_Generate_Unique_Codes_Under_Lock()
    {
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _generator.GenerateAsync(Context("Product", "PROD")))
            .ToArray();

        var codes = await Task.WhenAll(tasks);

        codes.Distinct().Count().ShouldBe(20);
        codes.OrderBy(x => x).First().ShouldBe("PROD-202607060001");
        codes.OrderBy(x => x).Last().ShouldBe("PROD-202607060020");
    }

    [Fact]
    public async Task Should_Not_Use_Legacy_Component_Terminology()
    {
        var material = await _generator.GenerateAsync(Context("Material", "MAT"));

        material.ShouldNotContain("Linh " + "kiện");
        material.ShouldBe("MAT-202607060001");
    }

    private static BusinessCodeGenerationContext Context(string sequenceName, string prefix) => new()
    {
        SequenceName = sequenceName,
        Prefix = prefix,
        Date = BusinessDate
    };

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; }

        public DateTimeKind Kind => DateTimeKind.Local;

        public bool SupportsMultipleTimezone => false;

        public DateTime Normalize(DateTime dateTime) => DateTime.SpecifyKind(dateTime, Kind);

        public DateTime ConvertToUserTime(DateTime dateTime) => Normalize(dateTime);

        public DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset) => dateTimeOffset;

        public DateTime ConvertToUtc(DateTime dateTime) => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    private sealed class InMemoryAbpDistributedLock : IAbpDistributedLock
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<IAbpDistributedLockHandle?> TryAcquireAsync(
            string name,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default)
        {
            var semaphore = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            var acquired = await semaphore.WaitAsync(timeout, cancellationToken);
            return acquired ? new Handle(semaphore) : null;
        }

        private sealed class Handle : IAbpDistributedLockHandle
        {
            private readonly SemaphoreSlim _semaphore;

            public Handle(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose() => _semaphore.Release();

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class TestDistributedCache : MemoryDistributedCache
    {
        public TestDistributedCache()
            : base(Options.Create(new MemoryDistributedCacheOptions()))
        {
        }
    }
}
