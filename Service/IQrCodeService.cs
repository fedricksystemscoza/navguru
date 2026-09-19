namespace NavGuru.Services;

public interface IQrCodeService
{
    /// <summary>
    /// Generates a signed, tamper-proof payload for a student + event combo.
    /// Format: userId|eventId|signature
    /// </summary>
    string GenerateCheckInPayload(string userId, int eventId);

    /// <summary>
    /// Validates a payload and extracts the student + event.
    /// Returns null if the signature is invalid or the payload is malformed.
    /// </summary>
    (string UserId, int EventId)? ValidateCheckInPayload(string payload);

    /// <summary>
    /// Renders a QR code as a PNG data URI (base64) suitable for &lt;img src="..." /&gt;.
    /// </summary>
    string GenerateQrPngDataUri(string payload, int pixelsPerModule = 10);
}