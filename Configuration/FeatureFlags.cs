namespace NavGuru.Configuration;

public class FeatureFlags
{
    public const string SectionName = "Features";

    public bool EnableAiAssistant { get; set; } = true;
    public bool EnableQrCheckIn { get; set; } = true;
    public bool EnableOfflineMode { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
}