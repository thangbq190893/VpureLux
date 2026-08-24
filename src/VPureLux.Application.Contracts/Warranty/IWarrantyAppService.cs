using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Warranty;

public interface IWarrantyAppService : IApplicationService
{
    Task<PagedResultDto<WarrantyPolicyListDto>> GetPolicyListAsync(GetWarrantyPolicyListInput input);

    Task<ComponentReplacementPolicyDto?> GetPolicyByComponentIdAsync(Guid componentId);

    Task<List<ComponentReplacementPolicyDto>> GetPoliciesByComponentIdsAsync(IReadOnlyCollection<Guid> componentIds);

    Task<ComponentReplacementPolicyDto> SetPolicyAsync(Guid componentId, SetComponentReplacementPolicyDto input);

    Task<PagedResultDto<ProductMachineSettingListDto>> GetMachineSettingListAsync(GetProductMachineSettingListInput input);

    Task<ProductMachineSettingDto?> GetMachineSettingByProductIdAsync(Guid productId);

    Task<List<ProductMachineSettingDto>> GetMachineSettingsByProductIdsAsync(IReadOnlyCollection<Guid> productIds);

    Task<ProductMachineSettingDto> SetMachineSettingAsync(Guid productId, SetProductMachineSettingDto input);

    Task<PagedResultDto<CustomerCareSyncFailureListDto>> GetSyncFailureListAsync(GetCustomerCareSyncFailureListInput input);

    Task RetrySyncFailureAsync(Guid id);

    Task<PagedResultDto<PendingInstallationListDto>> GetPendingInstallationListAsync(GetPendingInstallationListInput input);

    Task<AssetInstallationEditorDto> GetInstallationEditorAsync(Guid id);

    Task<ConfirmAssetInstallationResultDto> ConfirmInstallationAsync(Guid id, ConfirmAssetInstallationDto input);

    Task<PagedResultDto<CustomerAssetListDto>> GetAssetListAsync(GetCustomerAssetListInput input);

    Task<CustomerAssetDetailDto> GetAssetDetailsAsync(Guid id);

    Task<ExternalCustomerAssetResultDto> CreateExternalAssetAsync(CreateExternalCustomerAssetDto input);

    Task<ExternalCustomerAssetResultDto> UpdateExternalAssetAsync(Guid id, UpdateExternalCustomerAssetDto input);

    Task<PagedResultDto<AssetMaintenanceEventListDto>> GetAssetHistoryAsync(GetAssetMaintenanceHistoryInput input);

    Task<PagedResultDto<WarrantyReminderListDto>> GetReminderListAsync(GetWarrantyReminderListInput input);

    Task<WarrantyNotificationSummaryDto> GetNotificationSummaryAsync();

    Task CompleteReminderAsync(Guid id, CompleteReplacementReminderDto input);

    Task SkipReminderAsync(Guid id, SkipReplacementReminderDto input);

    Task RescheduleReminderAsync(Guid id, RescheduleReplacementReminderDto input);

    Task SuspendAssetAsync(Guid id, SuspendCustomerAssetDto input);
}
