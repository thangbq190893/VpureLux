using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace VPureLux.Catalog.Products;

public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductDto>> GetListAsync(GetProductListInput input);

    Task<ProductDto> GetAsync(Guid id);

    Task<ProductDto> CreateAsync(CreateProductDto input);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input);

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
