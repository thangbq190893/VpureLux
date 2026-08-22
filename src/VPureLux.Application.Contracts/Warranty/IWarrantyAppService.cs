using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Warranty;

public interface IWarrantyAppService : IApplicationService
{
    Task<PagedResultDto<WarrantyPolicyListDto>> GetPolicyListAsync(GetWarrantyPolicyListInput input);

    Task<ComponentReplacementPolicyDto?> GetPolicyByComponentIdAsync(Guid componentId);

    Task<ComponentReplacementPolicyDto> SetPolicyAsync(Guid componentId, SetComponentReplacementPolicyDto input);

    Task<PagedResultDto<WarrantyReminderListDto>> GetReminderListAsync(GetWarrantyReminderListInput input);

    Task CompleteReminderAsync(Guid id, CompleteReplacementReminderDto input);

    Task SkipReminderAsync(Guid id, SkipReplacementReminderDto input);

    Task RescheduleReminderAsync(Guid id, RescheduleReplacementReminderDto input);
}
