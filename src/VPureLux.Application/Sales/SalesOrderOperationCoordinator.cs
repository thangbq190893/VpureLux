using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;

namespace VPureLux.Sales;

public class SalesOrderOperationCoordinator : ITransientDependency
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SalesOrderOperationCoordinator(IAbpDistributedLock distributedLock, IUnitOfWorkManager unitOfWorkManager)
    {
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<T> ExecuteAsync<T>(Guid salesOrderId, Func<Task<T>> action)
    {
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            $"VPureLux:SalesOrder:{salesOrderId:N}", LockTimeout);
        if (lockHandle == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesPostConfirmationLockUnavailable)
                .WithData("SalesOrderId", salesOrderId);
        }

        using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var result = await action();
        await unitOfWork.CompleteAsync();
        return result;
    }

    public async Task ExecuteAsync(Guid salesOrderId, Func<Task> action) =>
        await ExecuteAsync(salesOrderId, async () =>
        {
            await action();
            return true;
        });
}
