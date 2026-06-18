using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace VPureLux.Catalog;

public static class CatalogImageTestData
{
    public static CatalogImageUploadDto Png(string fileName = "image.png", int width = 24, int height = 16) =>
        Create("image/png", fileName, width, height);

    public static CatalogImageUploadDto Jpeg(string fileName = "image.jpg") =>
        Create("image/jpeg", fileName, 24, 16);

    public static CatalogImageUploadDto Webp(string fileName = "image.webp") =>
        Create("image/webp", fileName, 24, 16);

    public static CatalogImageUploadDto Create(string mimeType, string fileName, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(10, 100, 200));
        using var stream = new MemoryStream();
        switch (mimeType)
        {
            case "image/jpeg":
                image.Save(stream, new JpegEncoder());
                break;
            case "image/png":
                image.Save(stream, new PngEncoder());
                break;
            case "image/webp":
                image.Save(stream, new WebpEncoder());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mimeType));
        }

        return new CatalogImageUploadDto
        {
            ImageBase64 = Convert.ToBase64String(stream.ToArray()),
            MimeType = mimeType,
            FileName = fileName
        };
    }
}
