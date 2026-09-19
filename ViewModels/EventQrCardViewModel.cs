using NavGuru.Models;

namespace NavGuru.ViewModels;

public class EventQrCardViewModel
{
    public OrientationEvent Event { get; set; } = null!;
    public string QrImageDataUri { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public bool AlreadyCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}