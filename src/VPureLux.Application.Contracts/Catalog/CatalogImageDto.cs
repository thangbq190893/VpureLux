namespace VPureLux.Catalog;

public class CatalogImageDto
{
    public byte[] Content { get; set; } = [];
    public string MimeType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ImageHash { get; set; } = string.Empty;
}
