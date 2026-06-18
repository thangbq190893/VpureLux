using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volo.Abp;

namespace VPureLux.Web.Pages.Catalog;

internal static class CatalogImageUploadHelper
{
    public static async Task<global::VPureLux.Catalog.CatalogImageUploadDto?> ToDtoAsync(IFormFile? image)
    {
        if (image == null || image.Length == 0)
        {
            return null;
        }

        if (image.Length > global::VPureLux.Catalog.CatalogConsts.MaxDecodedImageSize)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageTooLarge);
        }

        await using var stream = new MemoryStream();
        await image.CopyToAsync(stream);
        return new global::VPureLux.Catalog.CatalogImageUploadDto
        {
            ImageBase64 = Convert.ToBase64String(stream.ToArray()),
            MimeType = image.ContentType,
            FileName = Path.GetFileName(image.FileName)
        };
    }
}
