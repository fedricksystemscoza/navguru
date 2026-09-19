using NavGuru.ViewModels;

namespace NavGuru.Services;

public interface IFaqAssistantService
{
    Task<FaqAnswerResult> AskAsync(string question, List<FaqChatMessage>? history = null, CancellationToken ct = default);
}