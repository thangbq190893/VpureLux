using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;

namespace VPureLux.Warranty;

public class CustomerAssetOperationCoordinator(
    IAbpDistributedLock distributedLock, IUnitOfWorkManager unitOfWorkManager) : ITransientDependency
{
    public async Task<T> ExecuteAsync<T>(Guid assetId, Func<Task<T>> action)
    {
        var key = $"VPureLux:CustomerAsset:{assetId:N}";
        await using var handle = await distributedLock.TryAcquireAsync(key, TimeSpan.FromSeconds(30));
        if (handle == null)
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData("CustomerAssetId", assetId);
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        uow.Items[key] = handle;
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    public async Task HoldAsync(Guid assetId)
    {
        var uow = unitOfWorkManager.Current ?? throw new AbpException("Asset coordination requires a unit of work.");
        var key = $"VPureLux:CustomerAsset:{assetId:N}";
        if (uow.Items.ContainsKey(key)) return;
        var handle = await distributedLock.TryAcquireAsync(key, TimeSpan.FromSeconds(30));
        if (handle == null)
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData("CustomerAssetId", assetId)
                .WithData("Reason", "ConcurrentAssetOperation");
        uow.Items[key] = handle;
        // UoW disposal follows commit OR rollback, including failures in SaveChanges/local events.
        uow.Disposed += (_, _) => handle.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
