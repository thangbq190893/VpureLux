using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using QRCoder;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Web.Sales;

public sealed record SalesOrderPublicLinkPayload(Guid SalesOrderId, bool ShowPrices);

public class SalesOrderPublicLinkService : ITransientDependency
{
    private const string Purpose = "VPureLux.Sales.PublicOrderLink.v1";
    private readonly IDataProtector _protector;

    public SalesOrderPublicLinkService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string CreateToken(Guid salesOrderId, bool showPrices)
    {
        var payload = $"1|{salesOrderId:N}|{(showPrices ? "1" : "0")}";
        return _protector.Protect(payload);
    }

    public bool TryReadToken(string? token, out SalesOrderPublicLinkPayload payload)
    {
        payload = new SalesOrderPublicLinkPayload(Guid.Empty, false);
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            if (parts.Length != 3 ||
                parts[0] != "1" ||
                !Guid.TryParseExact(parts[1], "N", out var salesOrderId))
            {
                return false;
            }

            payload = new SalesOrderPublicLinkPayload(salesOrderId, parts[2] == "1");
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string CreateQrPngDataUri(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(8);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
