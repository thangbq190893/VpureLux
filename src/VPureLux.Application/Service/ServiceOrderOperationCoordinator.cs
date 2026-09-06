using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Data;
using Volo.Abp.Uow;
using VPureLux.Warranty;

namespace VPureLux.Service;

public class ServiceOrderOperationCoordinator(
    IAbpDistributedLock distributedLock, IUnitOfWorkManager unitOfWorkManager,
    IServiceOrderRepository orders, CustomerAssetOperationCoordinator assetCoordinator) : ITransientDependency
{
    public async Task<T> ExecuteAsync<T>(Guid orderId, Func<Task<T>> action, bool coordinateAsset = false)
    {
        await using var handle = await distributedLock.TryAcquireAsync(
            $"VPureLux:ServiceOrder:{orderId:N}", TimeSpan.FromSeconds(30));
        if (handle == null) throw Conflict(orderId);
        try
        {
            if (coordinateAsset)
            {
                Guid assetId;
                using (var lookup = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false))
                {
                    assetId = (await orders.GetAsync(orderId, includeDetails: false)).CustomerAssetId;
                    await lookup.CompleteAsync();
                }
                // Resolve the immutable asset identity first; acquire both locks before opening the business transaction.
                return await assetCoordinator.ExecuteAsync(assetId, action);
            }
            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var result = await action();
            await uow.CompleteAsync();
            return result;
        }
        catch (AbpDbConcurrencyException exception)
        {
            throw Conflict(orderId, exception);
        }
    }

    private static BusinessException Conflict(Guid id, Exception? inner = null) =>
        new BusinessException(ServiceErrorCodes.ConcurrentModification, innerException: inner).WithData("ServiceOrderId", id);
}
