using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using global::VPureLux.Audit;
using VPureLux.Localization;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Audit;

[Authorize(VPureLuxPermissions.Audit.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IBusinessAuditAppService _service;
    private readonly IAuthorizationService _authorization;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;
    [BindProperty(SupportsGet = true)] public AuditSearchInput Input { get; set; } = new();
    public bool CanExport { get; private set; }

    public IndexModel(
        IBusinessAuditAppService service,
        IAuthorizationService authorization,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _service = service;
        _authorization = authorization;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        CanExport = (await _authorization.AuthorizeAsync(User, VPureLuxPermissions.Audit.Export)).Succeeded;
    }

    public async Task<JsonResult> OnGetListAsync(AuditSearchInput input)
    {
        var result = await _service.GetListAsync(input);
        return new JsonResult(new PagedResultDto<AuditLogRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    private AuditLogRow ToRow(BusinessAuditLogDto log) => new(
        log.Id,
        log.EventTime.ToString("dd/MM/yyyy HH:mm:ss"),
        log.Module,
        GetActionLabel(log),
        log.EventName,
        GetPrimaryEntityLabel(log),
        log.EntityType,
        GetSeverityLabel(log),
        GetSeverityBadgeClass(log),
        GetGeneratedStatusLabel(log),
        GetGeneratedStatusBadgeClass(log),
        GetActorTypeLabel(log));

    private string GetActionLabel(BusinessAuditLogDto log)
    {
        var localized = _localizer[AuditUiFormatter.GetActionLocalizationKey(log.Action)];
        return localized.ResourceNotFound ? log.Action : localized.Value;
    }

    private string GetActorTypeLabel(BusinessAuditLogDto log) => _localizer[AuditUiFormatter.GetActorTypeLocalizationKey(log.ActorType)].Value;

    private string GetGeneratedStatusLabel(BusinessAuditLogDto log) =>
        _localizer[AuditUiFormatter.GetGeneratedStatusLocalizationKey(log)].Value;

    private string GetSeverityLabel(BusinessAuditLogDto log) =>
        _localizer[AuditUiFormatter.GetSeverityLocalizationKey(log.Severity)].Value;

    private string GetSeverityBadgeClass(BusinessAuditLogDto log) => AuditUiFormatter.GetSeverityBadgeClass(log.Severity);

    private string GetGeneratedStatusBadgeClass(BusinessAuditLogDto log) =>
        AuditUiFormatter.GetGeneratedStatusBadgeClass(log);

    private string GetPrimaryEntityLabel(BusinessAuditLogDto log) => AuditUiFormatter.GetPrimaryEntityLabel(log);

    public sealed record AuditLogRow(
        Guid Id,
        string EventTime,
        string Module,
        string ActionLabel,
        string EventName,
        string EntityLabel,
        string EntityType,
        string SeverityLabel,
        string SeverityBadgeClass,
        string StatusLabel,
        string StatusBadgeClass,
        string ActorTypeLabel);
}
