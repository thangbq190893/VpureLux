using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Components;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Auditing;

namespace VPureLux.Catalog;

[Route("api/catalog/components")]
public class ComponentController : AbpControllerBase
{
    private readonly IComponentAppService _componentAppService;

    public ComponentController(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    [HttpGet]
    public async Task<CatalogApiResponse<PagedResultDto<ComponentDto>>> GetListAsync(
        int page = 1,
        int pageSize = 10,
        string? keyword = null)
    {
        var result = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            SkipCount = Math.Max(0, page - 1) * pageSize,
            MaxResultCount = pageSize,
            Keyword = keyword
        });

        return CatalogApiResponse<PagedResultDto<ComponentDto>>.From(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<CatalogApiResponse<ComponentDto>> GetAsync(Guid id) =>
        CatalogApiResponse<ComponentDto>.From(await _componentAppService.GetAsync(id));

    [HttpPost]
    public async Task<CatalogApiResponse<ComponentDto>> CreateAsync(CreateComponentDto input) =>
        CatalogApiResponse<ComponentDto>.From(await _componentAppService.CreateAsync(input));

    [HttpPut("{id:guid}")]
    public async Task<CatalogApiResponse<ComponentDto>> UpdateAsync(Guid id, UpdateComponentDto input) =>
        CatalogApiResponse<ComponentDto>.From(await _componentAppService.UpdateAsync(id, input));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<CatalogApiResponse<bool>> DeactivateAsync(Guid id)
    {
        await _componentAppService.DeactivateAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<CatalogApiResponse<bool>> ActivateAsync(Guid id)
    {
        await _componentAppService.ActivateAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    [HttpGet("{id:guid}/image")]
    [DisableAuditing]
    public async Task<IActionResult> GetImageAsync(Guid id) =>
        ToImageResult(await _componentAppService.GetImageAsync(id));

    [HttpGet("{id:guid}/thumbnail")]
    [DisableAuditing]
    public async Task<IActionResult> GetThumbnailAsync(Guid id) =>
        ToImageResult(await _componentAppService.GetThumbnailAsync(id));

    [HttpPut("{id:guid}/image")]
    [DisableAuditing]
    public async Task<CatalogApiResponse<CatalogImageMetadataDto>> SetImageAsync(
        Guid id,
        [FromBody] CatalogImageUploadDto input) =>
        CatalogApiResponse<CatalogImageMetadataDto>.From(await _componentAppService.SetImageAsync(id, input));

    [HttpDelete("{id:guid}/image")]
    public async Task<CatalogApiResponse<bool>> RemoveImageAsync(Guid id)
    {
        await _componentAppService.RemoveImageAsync(id);
        return CatalogApiResponse<bool>.From(true);
    }

    private FileContentResult ToImageResult(CatalogImageDto image)
    {
        Response.Headers.ETag = $"\"{image.ImageHash}\"";
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(image.Content, image.MimeType, image.FileName);
    }
}
