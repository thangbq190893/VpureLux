namespace VPureLux.Catalog;

public static class CatalogConsts
{
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 500;
    public const int MaxUnitLength = 20;
    public const int MaxImageMimeTypeLength = 50;
    public const int MaxImageFileNameLength = 255;
    public const int ImageHashLength = 64;
    public const int MaxDecodedImageSize = 2 * 1024 * 1024;
    public const int ThumbnailSize = 96;
    public const int ThumbnailWebpQuality = 80;

    public static readonly string[] SupportedImageMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
