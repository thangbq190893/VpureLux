using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using global::VPureLux.Audit;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Audit;

[Authorize(VPureLuxPermissions.Audit.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly IBusinessAuditAppService _service;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public BusinessAuditLogDto Log { get; private set; } = new();
    public DetailsModel(IBusinessAuditAppService service) => _service = service;
    public async Task OnGetAsync() => Log = await _service.GetAsync(Id);

    public string PrimaryEntityLabel => AuditUiFormatter.GetPrimaryEntityLabel(Log);
    public bool HasEntityDisplay => !string.IsNullOrWhiteSpace(Log.EntityDisplay);
    public string OldValueJson => AuditUiFormatter.FormatJson(Log.OldValueJson);
    public string NewValueJson => AuditUiFormatter.FormatJson(Log.NewValueJson);
    public string MetadataJson => AuditUiFormatter.FormatJson(Log.MetadataJson);

    public string ActionLabel
    {
        get
        {
            var localized = L[AuditUiFormatter.GetActionLocalizationKey(Log.Action)];
            return localized.ResourceNotFound ? Log.Action : localized.Value;
        }
    }

    public string ActorTypeLabel => L[AuditUiFormatter.GetActorTypeLocalizationKey(Log.ActorType)].Value;
    public string GeneratedStatusLabel => L[AuditUiFormatter.GetGeneratedStatusLocalizationKey(Log)].Value;
    public string SeverityLabel => L[AuditUiFormatter.GetSeverityLocalizationKey(Log.Severity)].Value;
    public string SeverityBadgeClass => AuditUiFormatter.GetSeverityBadgeClass(Log.Severity);
    public string GeneratedStatusBadgeClass => AuditUiFormatter.GetGeneratedStatusBadgeClass(Log);
}
