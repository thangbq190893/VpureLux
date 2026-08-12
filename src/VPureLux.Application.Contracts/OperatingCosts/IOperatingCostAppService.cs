using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.OperatingCosts;

public interface IOperatingCostAppService : IApplicationService
{
    Task<PagedResultDto<OperatingCostCategoryDto>> GetCategoryListAsync(GetOperatingCostCategoryListInput input);

    Task<List<OperatingCostCategoryDto>> GetActiveCategoriesAsync(OperatingCostDirection? direction = null);

    Task<OperatingCostCategoryDto> GetCategoryAsync(Guid id);

    Task<OperatingCostCategoryDto> CreateCategoryAsync(CreateOperatingCostCategoryDto input);

    Task<OperatingCostCategoryDto> UpdateCategoryAsync(Guid id, UpdateOperatingCostCategoryDto input);

    Task DeleteCategoryAsync(Guid id);

    Task<PagedResultDto<OperatingCostEntryDto>> GetEntryListAsync(GetOperatingCostEntryListInput input);

    Task<OperatingCostSummaryDto> GetSummaryAsync(GetOperatingCostEntryListInput input);

    Task<OperatingCostEntryDto> GetEntryAsync(Guid id);

    Task<OperatingCostEntryDto> CreateEntryAsync(CreateOperatingCostEntryDto input);

    Task<OperatingCostEntryDto> UpdateEntryAsync(Guid id, UpdateOperatingCostEntryDto input);

    Task DeleteEntryAsync(Guid id);
}
