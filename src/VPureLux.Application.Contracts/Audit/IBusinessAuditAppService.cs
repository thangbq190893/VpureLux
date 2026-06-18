using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Audit;

public interface IBusinessAuditAppService : IApplicationService
{
    Task<PagedResultDto<BusinessAuditLogDto>> GetListAsync(AuditSearchInput input);
    Task<BusinessAuditLogDto> GetAsync(Guid id);
    Task<PagedResultDto<BusinessAuditLogDto>> GetReportAsync(string report, AuditSearchInput input);
    Task<AuditExportDto> ExportAsync(AuditSearchInput input);
}
