using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Bom;

[Route("api/bom")]
public class BomController : AbpControllerBase
{
    private readonly IBomAppService _bomAppService;

    public BomController(IBomAppService bomAppService)
    {
        _bomAppService = bomAppService;
    }

    [HttpGet("products/{productId:guid}/versions")]
    public Task<List<BomVersionDto>> GetListAsync(Guid productId)
    {
        return _bomAppService.GetListAsync(productId);
    }

    [HttpGet("versions/{id:guid}")]
    public Task<BomVersionDto> GetAsync(Guid id)
    {
        return _bomAppService.GetAsync(id);
    }

    [HttpPost("products/{productId:guid}/versions")]
    public Task<BomVersionDto> CreateAsync(Guid productId, [FromBody] CreateBomVersionDto input)
    {
        return _bomAppService.CreateAsync(productId, input);
    }

    [HttpPost("versions/{id:guid}/publish")]
    public Task PublishAsync(Guid id)
    {
        return _bomAppService.PublishAsync(id);
    }

    [HttpPost("versions/{id:guid}/archive")]
    public Task ArchiveAsync(Guid id)
    {
        return _bomAppService.ArchiveAsync(id);
    }

    [HttpPost("versions/{id:guid}/clone")]
    public Task<CloneBomVersionResultDto> CloneAsync(Guid id, [FromBody] CloneBomVersionDto input)
    {
        return _bomAppService.CloneAsync(id, input);
    }
}
