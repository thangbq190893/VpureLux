using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;

namespace VPureLux;

public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    public IDistributedLock CreateLock(string name) => new InMemoryDistributedLock(name);

    private sealed class InMemoryDistributedLock : IDistributedLock
    {
        public string Name { get; }

        public InMemoryDistributedLock(string name)
        {
            Name = name;
        }

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            new InMemorySynchronizationHandle();

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            new InMemorySynchronizationHandle();

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle?>(new InMemorySynchronizationHandle());

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle>(new InMemorySynchronizationHandle());
    }

    private sealed class InMemorySynchronizationHandle : IDistributedSynchronizationHandle
    {
        public CancellationToken HandleLostToken => CancellationToken.None;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
