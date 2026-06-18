using System;
using System.Linq;
using Shouldly;
using SixLabors.ImageSharp;
using Volo.Abp;
using Xunit;

namespace VPureLux.Catalog;

public class CatalogImageProcessorTests
{
    private readonly CatalogImageProcessor _processor = new();

    [Theory]
    [InlineData("image/jpeg", "valid.jpg")]
    [InlineData("image/png", "valid.png")]
    [InlineData("image/webp", "valid.webp")]
    public void Should_Process_Supported_Images(string mimeType, string fileName)
    {
        var input = CatalogImageTestData.Create(mimeType, fileName, 24, 16);

        var result = _processor.Process(input.ImageBase64, input.MimeType, input.FileName);

        result.MimeType.ShouldBe(mimeType);
        result.ImageHash.Length.ShouldBe(64);
        result.ImageHash.ShouldAllBe(x => char.IsAsciiHexDigit(x));
    }

    [Theory]
    [InlineData("", "image/png", "image.png", VPureLuxDomainErrorCodes.CatalogImageInvalidBase64)]
    [InlineData("not-base64", "image/png", "image.png", VPureLuxDomainErrorCodes.CatalogImageInvalidBase64)]
    [InlineData("AQID", "image/gif", "image.gif", VPureLuxDomainErrorCodes.CatalogImageUnsupportedFormat)]
    [InlineData("AQID", "image/jpeg", "image.png", VPureLuxDomainErrorCodes.CatalogImageUnsupportedFormat)]
    [InlineData("AQID", "image/jpeg", "image.jpg", VPureLuxDomainErrorCodes.CatalogImageInvalidSignature)]
    public void Should_Reject_Invalid_Input(string content, string mimeType, string fileName, string code)
    {
        var exception = Should.Throw<BusinessException>(() => _processor.Process(content, mimeType, fileName));
        exception.Code.ShouldBe(code);
    }

    [Fact]
    public void Should_Reject_Unsafe_Image_Content()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 1, 2, 3, 4 };

        var exception = Should.Throw<BusinessException>(() =>
            _processor.Process(Convert.ToBase64String(bytes), "image/jpeg", "image.jpg"));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.CatalogImageUnsafeContent);
    }

    [Fact]
    public void Should_Reject_Image_Above_Two_Megabytes()
    {
        var bytes = new byte[CatalogConsts.MaxDecodedImageSize + 1];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;

        var exception = Should.Throw<BusinessException>(() =>
            _processor.Process(Convert.ToBase64String(bytes), "image/jpeg", "large.jpg"));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.CatalogImageTooLarge);
    }

    [Fact]
    public void Should_Create_Dynamic_96x96_Bounded_Webp_Thumbnail()
    {
        var input = CatalogImageTestData.Png(width: 240, height: 160);
        var image = _processor.Process(input.ImageBase64, input.MimeType, input.FileName);

        var first = _processor.CreateThumbnail(image);
        var second = _processor.CreateThumbnail(image);
        var info = Image.Identify(first);

        info.ShouldNotBeNull();
        info.Width.ShouldBe(96);
        info.Height.ShouldBe(64);
        first.Take(4).ToArray().ShouldBe("RIFF"u8.ToArray());
        first.Skip(8).Take(4).ToArray().ShouldBe("WEBP"u8.ToArray());
        second.ShouldBe(first);
    }
}
