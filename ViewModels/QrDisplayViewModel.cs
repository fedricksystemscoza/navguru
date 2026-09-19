namespace NavGuru.ViewModels;

public class QrDisplayViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public DateTime EventStart { get; set; }
    public DateTime EventEnd { get; set; }

    /// <summary>The full payload that gets embedded in the QR code.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Base64 PNG data URI for the QR image (&lt;img src="..." /&gt;).</summary>
    public string QrImage { get; set; } = string.Empty;

    /// <summary>Short human-readable code students can type manually if the camera fails.</summary>
    public string ManualCode { get; set; } = string.Empty;

    /// <summary>Live count of students who have checked in.</summary>
    public int CheckedInCount { get; set; }
}