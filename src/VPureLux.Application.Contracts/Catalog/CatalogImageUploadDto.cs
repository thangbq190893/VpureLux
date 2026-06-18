using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;

namespace VPureLux.Catalog;

public class CatalogImageUploadDto
{
    [Required]
    [DisableAuditing]
    public string ImageBase64 { get; set; } = string.Empty;

    [Required]
    [StringLength(CatalogConsts.MaxImageMimeTypeLength)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [StringLength(CatalogConsts.MaxImageFileNameLength)]
    public string FileName { get; set; } = string.Empty;
}
