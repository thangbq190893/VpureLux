namespace VPureLux.Audit;

public static class AuditActionTypes
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Activate = "ACTIVATE";
    public const string Deactivate = "DEACTIVATE";
    public const string Publish = "PUBLISH";
    public const string Archive = "ARCHIVE";
    public const string Clone = "CLONE";
    public const string Post = "POST";
    public const string Confirm = "CONFIRM";
    public const string Cancel = "CANCEL";
    public const string ImageChange = "IMAGE_CHANGE";
    public const string ImageRemove = "IMAGE_REMOVE";
    public const string ExportRequested = "AUDIT_EXPORT_REQUESTED";
    public const string ExportCompleted = "AUDIT_EXPORT_COMPLETED";
}
