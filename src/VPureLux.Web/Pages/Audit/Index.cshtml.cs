using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using global::VPureLux.Audit;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Audit;

[Authorize(VPureLuxPermissions.Audit.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IBusinessAuditAppService _service;
    private readonly IAuthorizationService _authorization;
    [BindProperty(SupportsGet = true)] public AuditSearchInput Input { get; set; } = new() { MaxResultCount = 100 };
    public IReadOnlyList<BusinessAuditLogDto> Logs { get; private set; } = Array.Empty<BusinessAuditLogDto>();
    public bool CanExport { get; private set; }

    public IndexModel(IBusinessAuditAppService service, IAuthorizationService authorization)
    {
        _service = service;
        _authorization = authorization;
    }

    public async Task OnGetAsync()
    {
        Logs = (await _service.GetListAsync(Input)).Items;
        CanExport = (await _authorization.AuthorizeAsync(User, VPureLuxPermissions.Audit.Export)).Succeeded;
    }

    public string GetActionLabel(BusinessAuditLogDto log)
    {
        var localized = L[AuditUiFormatter.GetActionLocalizationKey(log.Action)];
        return localized.ResourceNotFound ? log.Action : localized.Value;
    }

    public string GetActorTypeLabel(BusinessAuditLogDto log) => L[AuditUiFormatter.GetActorTypeLocalizationKey(log.ActorType)].Value;

    public string GetGeneratedStatusLabel(BusinessAuditLogDto log) =>
        L[AuditUiFormatter.GetGeneratedStatusLocalizationKey(log)].Value;

    public string GetSeverityLabel(BusinessAuditLogDto log) =>
        L[AuditUiFormatter.GetSeverityLocalizationKey(log.Severity)].Value;

    public string GetSeverityBadgeClass(BusinessAuditLogDto log) => AuditUiFormatter.GetSeverityBadgeClass(log.Severity);

    public string GetGeneratedStatusBadgeClass(BusinessAuditLogDto log) =>
        AuditUiFormatter.GetGeneratedStatusBadgeClass(log);

    public string GetPrimaryEntityLabel(BusinessAuditLogDto log) => AuditUiFormatter.GetPrimaryEntityLabel(log);
}
