using Microsoft.Extensions.Options;
using NavGuru.Configuration;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using static NodaTime.TimeZones.ZoneEqualityComparer;

namespace NavGuru.Services;

public class QrCodeService : IQrCodeService
{
    private readonly string _secret;

    public QrCodeService(IOptions<QrOptions> options)
    {
        _secret = options.Value.SigningSecret;
        if (string.IsNullOrWhiteSpace(_secret))
            throw new InvalidOperationException(
                "Qr:SigningSecret is not configured. Set it via user-secrets or appsettings.json.");
    }

    public string GenerateCheckInPayload(string userId, int eventId)
    {
        var data = $"{userId}|{eventId}";
        var signature = Sign(data);
        return $"{data}|{signature}";
    }

    public (string UserId, int EventId)? ValidateCheckInPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        var parts = payload.Split('|');
        if (parts.Length != 3) return null;

        var userId = parts[0];
        if (!int.TryParse(parts[1], out var eventId)) return null;
        var providedSig = parts[2];

        var expectedSig = Sign($"{userId}|{eventId}");

        // Constant-time comparison to prevent timing attacks
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSig),
                Encoding.UTF8.GetBytes(expectedSig)))
            return null;

        return (userId, eventId);
    }

    public string GenerateQrPngDataUri(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);

        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(pixelsPerModule);

        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private string Sign(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        // URL-safe base64 (no padding, no + or /)
        return Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}