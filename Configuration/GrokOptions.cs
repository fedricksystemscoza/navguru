namespace NavGuru.Configuration;

public class GrokOptions
{
    public const string SectionName = "Grok";

    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.x.ai/v1";
    public string Model { get; set; } = "grok-3-mini";
    public int MaxTokens { get; set; } = 500;
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSeconds { get; set; } = 20;
    public string SystemPrompt { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}