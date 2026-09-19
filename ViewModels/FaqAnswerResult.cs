namespace NavGuru.ViewModels;

public class FaqAnswerResult
{
    public string Answer { get; set; } = string.Empty;
    public List<int> SourceFaqIds { get; set; } = new();
    public bool UsedAi { get; set; }
    public bool FromCache { get; set; }
    public string? FallbackReason { get; set; }
}

public class FaqChatMessage
{
    public string Role { get; set; } = "user";     // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}