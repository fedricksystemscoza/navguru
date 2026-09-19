using Microsoft.AspNetCore.Identity;

namespace NavGuru.Models;

public class CheckIn
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int OrientationEventId { get; set; }
    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public string? QrPayload { get; set; }
}
