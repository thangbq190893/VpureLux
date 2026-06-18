using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Auditing;

namespace VPureLux.Catalog;

[Route("api/catalog/products")]
public class ProductController : AbpControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [HttpGet]
    public async Task<CatalogApiResponse<PagedResultDto<ProductDto>>> GetListAsync(
        int page = 1,
        int pageSize = 10,
        string? keyword = null)
    {
        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            SkipCount = Math.Max(0, page - 1) * pageSize,
            MaxResultCount = pageSize,
            Keyword = keyword
        });

        return CatalogApiResponse<PagedResultDto<ProductDto>>.From(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<CatalogApiResponse<ProductDto>> GetAsync(Guid id) =>
        CatalogApiResponse<ProductDto>.From(await _productAppService.GetAsync(id));

    [HttpPost]
    public async Task<CatalogApiResponse<ProductDto>> CreateAsync(CreateProductDto input) =>
        CatalogApiResponse<ProductDto>.From(await _productAppService.CreateAsync(input));

    [HttpPut("{id:guid}")]
    public async Task<CatalogApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto input) =>
        CatalogApiResponse<ProductDto>.From(await _productAppService.UpdateAsync(id, input));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<CatalogApiResponse<bool>> DeactivateAsync(Guid id)
    {
        await _productAppService.DeactivateAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<CatalogApiResponse<bool>> ActivateAsync(Guid id)
    {
        await _productAppService.ActivateAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    [HttpGet("{id:guid}/image")]
    [DisableAuditing]
    public async Task<IActionResult> GetImageAsync(Guid id) =>
        ToImageResult(await _productAppService.GetImageAsync(id));

    [HttpGet("{id:guid}/thumbnail")]
    [DisableAuditing]
    public async Task<IActionResult> GetThumbnailAsync(Guid id) =>
        ToImageResult(await _productAppService.GetThumbnailAsync(id));

    [HttpPut("{id:guid}/image")]
    [DisableAuditing]
    public async Task<CatalogApiResponse<CatalogImageMetadataDto>> SetImageAsync(
        Guid id,
        [FromBody] CatalogImageUploadDto input) =>
        CatalogApiResponse<CatalogImageMetadataDto>.From(await _productAppService.SetImageAsync(id, input));

    [HttpDelete("{id:guid}/image")]
    public async Task<CatalogApiResponse<bool>> RemoveImageAsync(Guid id)
    {
        await _productAppService.RemoveImageAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    private FileContentResult ToImageResult(CatalogImageDto image)
    {
        Response.Headers.ETag = $"\"{image.ImageHash}\"";
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(image.Content, image.MimeType, image.FileName);
    }
}
