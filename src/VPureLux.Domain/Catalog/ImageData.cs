using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Values;

namespace VPureLux.Catalog;

public class ImageData : ValueObject
{
    private static readonly Regex Sha256Regex = new(
        "^[A-Fa-f0-9]{64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [DisableAuditing]
    public string ImageBase64 { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ImageHash { get; private set; } = string.Empty;

    protected ImageData()
    {
    }

    public ImageData(string imageBase64, string mimeType, string fileName, string imageHash)
    {
        ImageBase64 = Check.NotNullOrWhiteSpace(imageBase64, nameof(imageBase64));
        MimeType = Check.NotNullOrWhiteSpace(mimeType, nameof(mimeType), CatalogConsts.MaxImageMimeTypeLength);
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), CatalogConsts.MaxImageFileNameLength);
        ImageHash = Check.NotNullOrWhiteSpace(imageHash, nameof(imageHash), CatalogConsts.ImageHashLength).ToUpperInvariant();

        if (!CatalogConsts.SupportedImageMimeTypes.Contains(MimeType))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsupportedFormat);
        }

        if (!Sha256Regex.IsMatch(ImageHash))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsafeContent);
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ImageBase64;
        yield return MimeType;
        yield return FileName;
        yield return ImageHash;
    }
}
