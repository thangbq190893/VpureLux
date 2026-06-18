using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Catalog;

public class CatalogImageProcessor : ICatalogImageProcessor, ITransientDependency
{
    public ImageData Process(string imageBase64, string mimeType, string fileName)
    {
        if (imageBase64.IsNullOrWhiteSpace())
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageInvalidBase64);
        }

        if (mimeType.IsNullOrWhiteSpace() || fileName.IsNullOrWhiteSpace())
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsupportedFormat);
        }

        var normalizedMimeType = mimeType.Trim().ToLowerInvariant();
        ValidateFormat(normalizedMimeType, fileName);

        byte[] content;
        try
        {
            content = Convert.FromBase64String(RemoveDataUrlPrefix(imageBase64));
        }
        catch (FormatException)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageInvalidBase64);
        }

        if (content.Length == 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageInvalidBase64);
        }

        if (content.Length > CatalogConsts.MaxDecodedImageSize)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageTooLarge)
                .WithData("MaxBytes", CatalogConsts.MaxDecodedImageSize);
        }

        if (!HasValidSignature(content, normalizedMimeType))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageInvalidSignature);
        }

        ValidateSafeImage(content);

        var hash = Convert.ToHexString(SHA256.HashData(content));
        return new ImageData(Convert.ToBase64String(content), normalizedMimeType, Path.GetFileName(fileName), hash);
    }

    public byte[] CreateThumbnail(ImageData image)
    {
        try
        {
            var content = Convert.FromBase64String(image.ImageBase64);
            using var source = Image.Load(content);
            source.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(CatalogConsts.ThumbnailSize, CatalogConsts.ThumbnailSize)
            }));

            using var output = new MemoryStream();
            source.Save(output, new WebpEncoder { Quality = CatalogConsts.ThumbnailWebpQuality });
            return output.ToArray();
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsafeContent);
        }
    }

    private static void ValidateFormat(string mimeType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = mimeType switch
        {
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/png" => new[] { ".png" },
            "image/webp" => new[] { ".webp" },
            _ => []
        };

        if (!CatalogConsts.SupportedImageMimeTypes.Contains(mimeType) ||
            !allowedExtensions.Contains(extension))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsupportedFormat);
        }
    }

    private static string RemoveDataUrlPrefix(string value)
    {
        var commaIndex = value.IndexOf(',');
        return value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
            ? value[(commaIndex + 1)..]
            : value;
    }

    private static bool HasValidSignature(byte[] content, string mimeType)
    {
        return mimeType switch
        {
            "image/jpeg" => content.Length >= 3 &&
                            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.Length >= 8 &&
                           content.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => content.Length >= 12 &&
                            content.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                            content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static void ValidateSafeImage(byte[] content)
    {
        try
        {
            var info = Image.Identify(content);
            if (info == null || info.Width <= 0 || info.Height <= 0 ||
                (long)info.Width * info.Height > 40_000_000)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsafeContent);
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CatalogImageUnsafeContent);
        }
    }
}
