using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Warranty;

public interface IComponentReplacementPolicyRepository : IRepository<ComponentReplacementPolicy, Guid>
{
    Task<ComponentReplacementPolicy?> FindByComponentIdAsync(Guid componentId, CancellationToken cancellationToken = default);

    Task<List<ComponentReplacementPolicy>> GetByComponentIdsAsync(
        IReadOnlyCollection<Guid> componentIds,
        CancellationToken cancellationToken = default);

    Task<List<ComponentReplacementPolicy>> GetEnabledByComponentIdsAsync(
        IReadOnlyCollection<Guid> componentIds,
        CancellationToken cancellationToken = default);
}

public interface IProductMachineSettingRepository : IRepository<ProductMachineSetting, Guid>
{
    Task<ProductMachineSetting?> FindByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<List<ProductMachineSetting>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}

public interface ICustomerCareSyncFailureRepository : IRepository<CustomerCareSyncFailure, Guid>
{
    Task<CustomerCareSyncFailure?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public interface ICustomerCareSalesIntakeRepository
{
    Task<List<CustomerCareSalesIntakeCandidate>> GetCandidatesAsync(
        DateTime confirmedFrom,
        DateTime retryDueAt,
        int maxResultCount,
        CancellationToken cancellationToken = default);
}

public interface IWarrantyReadRepository
{
    Task<long> GetPolicyCountAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default);

    Task<List<WarrantyPolicyListItem>> GetPolicyListAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetMachineSettingCountAsync(ProductMachineSettingFilter filter, CancellationToken cancellationToken = default);

    Task<List<ProductMachineSettingListItem>> GetMachineSettingListAsync(ProductMachineSettingFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetSyncFailureCountAsync(CustomerCareSyncFailureFilter filter, CancellationToken cancellationToken = default);

    Task<List<CustomerCareSyncFailureListItem>> GetSyncFailureListAsync(CustomerCareSyncFailureFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetPendingInstallationCountAsync(PendingInstallationFilter filter, CancellationToken cancellationToken = default);

    Task<List<PendingInstallationListItem>> GetPendingInstallationListAsync(PendingInstallationFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetAssetCountAsync(CustomerAssetFilter filter, CancellationToken cancellationToken = default);

    Task<List<CustomerAssetListItem>> GetAssetListAsync(CustomerAssetFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetAssetHistoryCountAsync(AssetMaintenanceHistoryFilter filter, CancellationToken cancellationToken = default);

    Task<List<AssetMaintenanceEventListItem>> GetAssetHistoryListAsync(AssetMaintenanceHistoryFilter filter, CancellationToken cancellationToken = default);

    Task<long> GetReminderCountAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default);

    Task<List<WarrantyReminderListItem>> GetReminderListAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default);

    Task<WarrantyNotificationSummary> GetNotificationSummaryAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default);
}
