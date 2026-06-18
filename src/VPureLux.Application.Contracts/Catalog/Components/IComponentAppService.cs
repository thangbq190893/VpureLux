using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace VPureLux.Catalog.Components;

public interface IComponentAppService : IApplicationService
{
    Task<PagedResultDto<ComponentDto>> GetListAsync(GetComponentListInput input);

    Task<ComponentDto> GetAsync(Guid id);

    Task<ComponentDto> CreateAsync(CreateComponentDto input);

    Task<ComponentDto> UpdateAsync(Guid id, UpdateComponentDto input);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);

    [DisableAuditing]
    Task<CatalogImageDto> GetImageAsync(Guid id);

    [DisableAuditing]
    Task<CatalogImageDto> GetThumbnailAsync(Guid id);

    [DisableAuditing]
    Task<CatalogImageMetadataDto> SetImageAsync(Guid id, CatalogImageUploadDto input);

    Task RemoveImageAsync(Guid id);
}
