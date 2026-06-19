using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using global::VPureLux.Audit;

namespace VPureLux.Web.Pages.Audit;

public static class AuditUiFormatter
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static string GetSeverityLocalizationKey(AuditSeverity severity) => $"Audit:Severity:{severity}";

    public static string GetActorTypeLocalizationKey(AuditActorType actorType) => $"Audit:ActorType:{actorType}";

    public static string GetActionLocalizationKey(string action) => $"Audit:Action:{action}";

    public static string GetGeneratedStatusLocalizationKey(BusinessAuditLogDto log) =>
        log.IsSystemGenerated ? "Audit:Status:SystemGenerated" : "Audit:Status:UserOrIntegration";

    public static string GetSeverityBadgeClass(AuditSeverity severity) => severity switch
    {
        AuditSeverity.Critical => "text-bg-danger",
        AuditSeverity.Important => "text-bg-warning",
        _ => "text-bg-info"
    };

    public static string GetGeneratedStatusBadgeClass(BusinessAuditLogDto log) =>
        log.IsSystemGenerated ? "text-bg-secondary" : "text-bg-success";

    public static string GetPrimaryEntityLabel(BusinessAuditLogDto log) =>
        string.IsNullOrWhiteSpace(log.EntityDisplay) ? log.EntityType : log.EntityDisplay;

    public static string FormatJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, IndentedJsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
