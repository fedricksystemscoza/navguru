using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NavGuru.Configuration;
using NavGuru.Data;
using NavGuru.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace NavGuru.Services;

public class FaqService
{
    private readonly ApplicationDbContext _db;
    private readonly OpenAiOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FaqService> _logger;

    public FaqService(
        ApplicationDbContext db,
        IOptions<OpenAiOptions> options,
        IMemoryCache cache,
        ILogger<FaqService> logger)
    {
        _db = db;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<FaqEntry>> GetAllApprovedAsync()
    {
        return await _cache.GetOrCreateAsync("approved-faqs", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.FaqEntries
                .Where(f => f.IsApproved)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.Question)
                .ToListAsync();
        }) ?? new List<FaqEntry>();
    }

    public async Task<FaqAnswer> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new FaqAnswer { Text = "Please type a question.", UsedAi = false };

        var faqs = await GetAllApprovedAsync();

        if (faqs.Count == 0)
            return new FaqAnswer
            {
                Text = "No FAQs have been added yet. Please contact Student Services.",
                UsedAi = false
            };

        // Try OpenAI first if configured
        if (_options.IsConfigured)
        {
            try
            {
                var answer = await AskOpenAiAsync(question, faqs);
                return new FaqAnswer { Text = answer, UsedAi = true };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI call failed, falling back to keyword match");
                // fall through to keyword match
            }
        }

        // Fallback: keyword matching
        var fallback = KeywordMatch(question, faqs);
        return new FaqAnswer { Text = fallback, UsedAi = false };
    }

    private async Task<string> AskOpenAiAsync(string question, List<FaqEntry> faqs)
    {
        var client = new OpenAIClient(new ApiKeyCredential(_options.ApiKey));
        var chat = client.GetChatClient(_options.ChatModel);

        // Build the FAQ context
        var faqContext = string.Join("\n\n", faqs.Select((f, i) =>
            $"[{i + 1}] Q: {f.Question}\nA: {f.Answer}"));

        var systemPrompt = $@"{_options.SystemPrompt}

You have access to this approved FAQ library:

{faqContext}

Answer the student's question using ONLY the information above.
If the answer isn't covered, say: ""I don't have that information — please contact Student Services at support@mandela.ac.za.""
Keep answers short and friendly. Don't repeat the question.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(question)
        };

        var response = await chat.CompleteChatAsync(messages, new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxTokens,
            Temperature = 0.3f
        });

        return response.Value.Content[0].Text;
    }

    private string KeywordMatch(string question, List<FaqEntry> faqs)
    {
        var words = question
            .ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', '?', '!', ';', ':' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToArray();

        if (words.Length == 0)
            return "Please ask a more specific question. Or contact Student Services at support@mandela.ac.za.";

        // Score each FAQ
        var scored = faqs.Select(f => new
        {
            Faq = f,
            Score = words.Count(w =>
                f.Question.ToLowerInvariant().Contains(w) ||
                f.Answer.ToLowerInvariant().Contains(w))
        })
        .OrderByDescending(x => x.Score)
        .FirstOrDefault();

        if (scored is null || scored.Score == 0)
            return "I couldn't find an answer to that. Please contact Student Services at support@mandela.ac.za.";

        return scored.Faq.Answer;
    }
}

public class FaqAnswer
{
    public string Text { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
}