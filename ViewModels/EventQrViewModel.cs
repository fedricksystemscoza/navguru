using NavGuru.Models;

namespace NavGuru.ViewModels;

public class EventQrViewModel
{
    /// <summary>The event this QR is for.</summary>
    public OrientationEvent Event { get; set; } = null!;

    /// <summary>The student's user ID (for logging/debugging).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Display name of the student.</summary>
    public string UserFullName { get; set; } = string.Empty;

    /// <summary>The signed payload embedded in the QR (userId|eventId|signature).</summary>
    public string QrPayload { get; set; } = string.Empty;

    /// <summary>Base64 PNG data URI for the QR image, ready for &lt;img src="..." /&gt;.</summary>
    public string QrImageDataUri { get; set; } = string.Empty;

    /// <summary>True if the student has already checked into this event.</summary>
    public bool AlreadyCheckedIn { get; set; }

    /// <summary>When the check-in happened (if already checked in).</summary>
    public DateTime? CheckedInAt { get; set; }
}