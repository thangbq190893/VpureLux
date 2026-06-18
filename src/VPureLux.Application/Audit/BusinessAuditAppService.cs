using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace VPureLux.Audit;

[Authorize(VPureLuxPermissions.Audit.View)]
public class BusinessAuditAppService : ApplicationService, IBusinessAuditAppService
{
    private readonly IBusinessAuditLogRepository _repository;
    private readonly BusinessAuditManager _manager;
    private readonly IAuditingManager _auditingManager;
    private readonly ICurrentUser _currentUser;

    public BusinessAuditAppService(
        IBusinessAuditLogRepository repository,
        BusinessAuditManager manager,
        IAuditingManager auditingManager,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _manager = manager;
        _auditingManager = auditingManager;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<BusinessAuditLogDto>> GetListAsync(AuditSearchInput input)
    {
        var filter = ToFilter(input);
        return new PagedResultDto<BusinessAuditLogDto>(
            await _repository.GetCountAsync(filter),
            (await _repository.GetListAsync(filter)).Select(ToDto).ToList());
    }

    public async Task<BusinessAuditLogDto> GetAsync(Guid id) =>
        ToDto(await _repository.FindAsync(id) ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound));

    public Task<PagedResultDto<BusinessAuditLogDto>> GetReportAsync(string report, AuditSearchInput input)
    {
        input.Module = report.ToLowerInvariant() switch
        {
            "price-changes" => "Pricing",
            "bom-changes" => "BOM",
            "inventory-actions" => "Inventory",
            "sales-actions" => "Sales",
            _ => input.Module
        };
        return GetListAsync(input);
    }

    [Authorize(VPureLuxPermissions.Audit.Export)]
    public async Task<AuditExportDto> ExportAsync(AuditSearchInput input)
    {
        var correlationId = CurrentCorrelationId();
        await WriteExportAuditAsync(AuditActionTypes.ExportRequested, correlationId, input);
        var rows = await _repository.GetListAsync(ToFilter(input) with { SkipCount = 0, MaxResultCount = 5000 });
        var builder = new StringBuilder("EventTime,Module,EventName,Action,EntityType,EntityId,Severity,User,CorrelationId\n");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Csv(row.EventTime.ToString("O")), Csv(row.Module), Csv(row.EventName), Csv(row.Action),
                Csv(row.EntityType), Csv(row.EntityId.ToString()), Csv(row.Severity.ToString()),
                Csv(row.UserNameSnapshot), Csv(row.CorrelationId)
            }));
        }
        await WriteExportAuditAsync(AuditActionTypes.ExportCompleted, correlationId, new { Count = rows.Count });
        return new AuditExportDto { Content = Encoding.UTF8.GetBytes(builder.ToString()) };
    }

    private async Task WriteExportAuditAsync(string action, string correlationId, object metadata)
    {
        var envelope = new BusinessAuditEnvelope(
            GuidGenerator.Create(), "Audit", action, action, "AuditExport", GuidGenerator.Create(),
            correlationId, correlationId, Clock.Now, AuditSeverity.Important,
            MetadataJson: JsonSerializer.Serialize(metadata), UserId: _currentUser.Id,
            UserName: _currentUser.UserName, ActorType: _currentUser.Id.HasValue ? AuditActorType.User : AuditActorType.System,
            IsSystemGenerated: !_currentUser.Id.HasValue);
        await _repository.InsertAsync(_manager.Create(envelope), autoSave: true);
    }

    private string CurrentCorrelationId() =>
        _auditingManager.Current?.Log.CorrelationId ?? GuidGenerator.Create().ToString("N");

    private static AuditSearchFilter ToFilter(AuditSearchInput input) => new(
        input.From, input.To, input.UserId, input.Module, input.EntityType, input.EntityId,
        input.Severity, input.CorrelationId, input.SkipCount, input.MaxResultCount);

    private static BusinessAuditLogDto ToDto(BusinessAuditLog x) => new()
    {
        Id = x.Id, EventId = x.EventId, Module = x.Module, EventName = x.EventName,
        EventVersion = x.EventVersion, Action = x.Action, EntityType = x.EntityType,
        EntityId = x.EntityId, EntityDisplay = x.EntityDisplay, ActorType = x.ActorType,
        UserId = x.UserId, UserNameSnapshot = x.UserNameSnapshot, EventTime = x.EventTime,
        CorrelationId = x.CorrelationId, CausationId = x.CausationId,
        OldValueJson = x.OldValueJson, NewValueJson = x.NewValueJson,
        MetadataJson = x.MetadataJson, Severity = x.Severity, IsSystemGenerated = x.IsSystemGenerated
    };

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
