using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VPureLux.CustomerCare;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace VPureLux.Warranty;

public sealed record CustomerCareSalesIntakeResult(
    int CandidateCount,
    int CreatedAssetCount,
    int FailedLineCount,
    bool GateEnabled,
    bool LockAcquired);

public class CustomerCareSalesIntakeService : ITransientDependency
{
    private const string LockName = "VPureLux:CustomerCare:SalesIntake";
    private static readonly TimeSpan LockTimeout = TimeSpan.Zero;

    private readonly IOptions<CustomerCareOptions> _options;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ICustomerCareSalesIntakeRepository _intakeRepository;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IRepository<CustomerAssetComponent, Guid> _assetComponents;
    private readonly ICustomerCareSyncFailureRepository _failures;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<CustomerCareSalesIntakeService> _logger;

    public CustomerCareSalesIntakeService(
        IOptions<CustomerCareOptions> options,
        IAbpDistributedLock distributedLock,
        ICustomerCareSalesIntakeRepository intakeRepository,
        IRepository<CustomerAsset, Guid> assets,
        IRepository<CustomerAssetComponent, Guid> assetComponents,
        ICustomerCareSyncFailureRepository failures,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<CustomerCareSalesIntakeService> logger)
    {
        _options = options;
        _distributedLock = distributedLock;
        _intakeRepository = intakeRepository;
        _assets = assets;
        _assetComponents = assetComponents;
        _failures = failures;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task<CustomerCareSalesIntakeResult> RunBatchAsync(
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var current = now ?? DateTimeOffset.UtcNow;
        var options = _options.Value;
        if (!options.CanRunSalesIntake(current))
        {
            return new CustomerCareSalesIntakeResult(0, 0, 0, false, false);
        }

        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            LockName,
            LockTimeout,
            cancellationToken);
        if (lockHandle == null)
        {
            return new CustomerCareSalesIntakeResult(0, 0, 0, true, false);
        }

        var batchSize = Math.Clamp(options.SalesIntakeBatchSize, 1, 1000);
        var candidates = await LoadCandidatesAsync(
            options.SalesIntakeGoLiveFrom!.Value.UtcDateTime,
            current.UtcDateTime,
            batchSize,
            cancellationToken);
        var createdAssetCount = 0;
        var failedLineCount = 0;

        foreach (var candidate in candidates)
        {
            try
            {
                createdAssetCount += await ProcessCandidateAsync(candidate, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedLineCount++;
                _logger.LogError(
                    exception,
                    "CustomerCare Sales intake failed for order {OrderNo}, line {LineNo}, SalesOrderId {SalesOrderId}, SalesOrderLineId {SalesOrderLineId}, Product {ProductCode}, Quantity {Quantity}",
                    candidate.OrderNo,
                    candidate.SalesOrderLineNo,
                    candidate.SalesOrderId,
                    candidate.SalesOrderLineId,
                    candidate.ProductCode,
                    candidate.Quantity);
                await RecordFailureAsync(candidate, exception, current.UtcDateTime, cancellationToken);
            }
        }

        return new CustomerCareSalesIntakeResult(
            candidates.Count,
            createdAssetCount,
            failedLineCount,
            true,
            true);
    }

    private async Task<System.Collections.Generic.List<CustomerCareSalesIntakeCandidate>> LoadCandidatesAsync(
        DateTime confirmedFrom,
        DateTime retryDueAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var candidates = await _intakeRepository.GetCandidatesAsync(
            confirmedFrom,
            retryDueAt,
            batchSize,
            cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return candidates;
    }

    private async Task<int> ProcessCandidateAsync(
        CustomerCareSalesIntakeCandidate candidate,
        CancellationToken cancellationToken)
    {
        var machineCount = ToPositiveInteger(candidate.Quantity, "SalesQuantity");
        var components = candidate.BomItems
            .Select((item, index) => new PreparedComponent(
                item,
                index + 1,
                ToPositiveInteger(item.QuantityPerProduct, $"BomQuantity:{item.ComponentCode}")))
            .ToList();

        using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        for (var unitIndex = 1; unitIndex <= machineCount; unitIndex++)
        {
            var asset = CustomerAsset.CreateSoldMachine(
                Guid.NewGuid(),
                candidate.CustomerId,
                candidate.ProductId,
                candidate.SalesOrderId,
                candidate.SalesOrderLineId,
                candidate.SalesOrderLineNo,
                unitIndex,
                CreateAssetNo(candidate.OrderNo, candidate.SalesOrderLineNo, unitIndex),
                candidate.OrderNo,
                candidate.CustomerCode,
                candidate.CustomerName,
                candidate.ProductCode,
                candidate.ProductName,
                candidate.SoldAt);
            await _assets.InsertAsync(asset, cancellationToken: cancellationToken);

            foreach (var component in components)
            {
                await _assetComponents.InsertAsync(new CustomerAssetComponent(
                    Guid.NewGuid(),
                    asset.Id,
                    $"BOM-{component.Position:D2}",
                    component.Item.ComponentName,
                    component.Item.ComponentId,
                    component.Item.ComponentCode,
                    component.Item.ComponentName,
                    component.Item.Unit,
                    component.Quantity,
                    null,
                    pendingInstallation: true), cancellationToken: cancellationToken);
            }
        }

        var failure = await _failures.FindByIdempotencyKeyAsync(FailureKey(candidate), cancellationToken);
        if (failure is { Status: CustomerCareSyncFailureStatus.Pending })
        {
            failure.Resolve(DateTime.UtcNow);
            await _failures.UpdateAsync(failure, cancellationToken: cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return machineCount;
    }

    private async Task RecordFailureAsync(
        CustomerCareSalesIntakeCandidate candidate,
        Exception exception,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        try
        {
            using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var key = FailureKey(candidate);
            var failure = await _failures.FindByIdempotencyKeyAsync(key, cancellationToken);
            var errorCode = exception is BusinessException businessException
                ? businessException.Code
                : exception.GetType().Name;
            var message = Truncate(exception.Message, WarrantyConsts.MaxErrorMessageLength);
            var context = Truncate(
                $"OrderNo={candidate.OrderNo};LineNo={candidate.SalesOrderLineNo};Product={candidate.ProductCode};Quantity={candidate.Quantity}",
                WarrantyConsts.MaxErrorContextLength);

            if (failure == null)
            {
                failure = new CustomerCareSyncFailure(
                    Guid.NewGuid(),
                    key,
                    message,
                    occurredAt,
                    candidate.SalesOrderId,
                    candidate.SalesOrderLineId,
                    Truncate(errorCode, WarrantyConsts.MaxErrorCodeLength),
                    context,
                    occurredAt.AddMinutes(5));
                await _failures.InsertAsync(failure, cancellationToken: cancellationToken);
            }
            else
            {
                failure.RecordAttempt(
                    Truncate(errorCode, WarrantyConsts.MaxErrorCodeLength),
                    message,
                    context,
                    occurredAt,
                    occurredAt.AddMinutes(5));
                await _failures.UpdateAsync(failure, cancellationToken: cancellationToken);
            }

            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception failureException)
        {
            _logger.LogError(
                failureException,
                "Could not persist CustomerCare sync failure for SalesOrderLineId {SalesOrderLineId}",
                candidate.SalesOrderLineId);
        }
    }

    private static int ToPositiveInteger(decimal value, string field)
    {
        if (value <= 0 || value != decimal.Truncate(value) || value > int.MaxValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData("Field", field)
                .WithData("Value", value);
        }

        return decimal.ToInt32(value);
    }

    private static string FailureKey(CustomerCareSalesIntakeCandidate candidate) =>
        $"sales-intake:{candidate.SalesOrderLineId:N}";

    private static string CreateAssetNo(string orderNo, int lineNo, int unitIndex) =>
        $"WA-{orderNo}-L{lineNo:D2}-{unitIndex:D2}";

    private static string Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Length <= maxLength ? value : value[..maxLength];

    private sealed record PreparedComponent(
        CustomerCareSalesIntakeBomItem Item,
        int Position,
        int Quantity);
}
