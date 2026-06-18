using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Audit;

[Route("api/audit")]
public class BusinessAuditController : AbpControllerBase
{
    private readonly IBusinessAuditAppService _service;
    public BusinessAuditController(IBusinessAuditAppService service) => _service = service;

    [HttpGet("logs")]
    public Task<PagedResultDto<BusinessAuditLogDto>> GetListAsync([FromQuery] AuditSearchInput input) =>
        _service.GetListAsync(input);

    [HttpGet("logs/{id:guid}")]
    public Task<BusinessAuditLogDto> GetAsync(Guid id) => _service.GetAsync(id);

    [HttpGet("entities/{entityType}/{entityId:guid}")]
    public Task<PagedResultDto<BusinessAuditLogDto>> GetEntityAsync(string entityType, Guid entityId, [FromQuery] AuditSearchInput input)
    {
        input.EntityType = entityType;
        input.EntityId = entityId;
        return _service.GetListAsync(input);
    }

    [HttpGet("correlations/{correlationId}")]
    public Task<PagedResultDto<BusinessAuditLogDto>> GetCorrelationAsync(string correlationId, [FromQuery] AuditSearchInput input)
    {
        input.CorrelationId = correlationId;
        return _service.GetListAsync(input);
    }

    [HttpGet("reports/{report}")]
    public Task<PagedResultDto<BusinessAuditLogDto>> GetReportAsync(string report, [FromQuery] AuditSearchInput input) =>
        _service.GetReportAsync(report, input);

    [HttpPost("exports")]
    public async Task<IActionResult> ExportAsync([FromBody] AuditSearchInput input)
    {
        var export = await _service.ExportAsync(input);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
