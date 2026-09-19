using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NavGuru.Configuration;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.ViewModels;

namespace NavGuru.Services;

public class FaqAssistantService : IFaqAssistantService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpFactory;
    private readonly GrokOptions _options;
    private readonly ILogger<FaqAssistantService> _logger;

    private const string FaqCacheKey = "faq:approved:v1";

    public FaqAssistantService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IHttpClientFactory httpFactory,
        IOptions<GrokOptions> options,
        ILogger<FaqAssistantService> logger)
    {
        _db = db;
        _cache = cache;
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FaqAnswerResult> AskAsync(
        string question,
        List<FaqChatMessage>? history = null,
        CancellationToken ct = default)
    {
        question = (question ?? "").Trim();
        if (question.Length < 2)
        {
            return new FaqAnswerResult
            {
                Answer = "Could you rephrase that as a full question?",
                UsedAi = false
            };
        }

        // ---- 1. Load approved FAQs (cached for 60 minutes) ----
        var faqs = await GetApprovedFaqsAsync(ct);

        // ---- 2. Score each FAQ by word overlap ----
        var ranked = RankFaqs(question, faqs).Take(5).ToList();

        // ---- 3. No good matches? Return a polite fallback ----
        if (ranked.Count == 0)
        {
            return new FaqAnswerResult
            {
                Answer = "I don't have that in my FAQ library yet. Try rephrasing, or contact Student Services on the Support page.",
                UsedAi = false,
                FallbackReason = "No matching FAQs"
            };
        }

        // ---- 4. If Grok isn't configured, just return the best FAQ answer ----
        if (!_options.IsConfigured)
        {
            var top = ranked.First();
            return new FaqAnswerResult
            {
                Answer = top.Answer,
                SourceFaqIds = ranked.Select(f => f.Id).ToList(),
                UsedAi = false,
                FallbackReason = "Grok API not configured — returning best FAQ match."
            };
        }

        // ---- 5. Build context + call Grok ----
        var context = string.Join("\n\n",
            ranked.Select((f, i) => $"[FAQ {i + 1}] Q: {f.Question}\nA: {f.Answer}"));

        var answer = await CallGrokAsync(question, context, history, ct);

        return new FaqAnswerResult
        {
            Answer = answer ?? "I couldn't process that right now. Please try again or check the FAQ list.",
            SourceFaqIds = ranked.Select(f => f.Id).ToList(),
            UsedAi = true
        };
    }

    // ---------------------------------------------------------
    // Load approved FAQs with 60-minute in-memory cache
    // ---------------------------------------------------------
    private async Task<List<FaqEntry>> GetApprovedFaqsAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(FaqCacheKey, out List<FaqEntry>? cached) && cached is not null)
            return cached;

        var faqs = await _db.FaqEntries
            .Where(f => f.IsApproved)
            .ToListAsync(ct);

        _cache.Set(FaqCacheKey, faqs, TimeSpan.FromMinutes(60));
        return faqs;
    }

    // ---------------------------------------------------------
    // Word-overlap scoring — cheap but effective RAG-lite
    // ---------------------------------------------------------
    private static IEnumerable<FaqEntry> RankFaqs(string query, List<FaqEntry> faqs)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","is","are","was","were","be","been","being",
            "i","you","he","she","it","we","they","me","my","your","our",
            "to","of","in","on","at","for","with","about","and","or","but",
            "do","does","did","can","could","would","should","will","how",
            "what","where","when","why","who","which","this","that","there"
        };

        var queryWords = query
            .ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', '?', '!', ';', ':', '\n', '\r', '\t' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stop.Contains(w))
            .ToHashSet();

        if (queryWords.Count == 0) yield break;

        var scored = new List<(FaqEntry Faq, int Score)>();
        foreach (var faq in faqs)
        {
            var faqText = (faq.Question + " " + faq.Answer + " " + faq.Category).ToLowerInvariant();
            var score = queryWords.Count(w => faqText.Contains(w));

            // Boost if the whole question phrase appears
            if (faqText.Contains(query.ToLowerInvariant())) score += 5;

            if (score > 0) scored.Add((faq, score));
        }

        foreach (var item in scored.OrderByDescending(x => x.Score))
            yield return item.Faq;
    }

    // ---------------------------------------------------------
    // Call Grok (OpenAI-compatible chat completions endpoint)
    // ---------------------------------------------------------
    private async Task<string?> CallGrokAsync(
        string question,
        string context,
        List<FaqChatMessage>? history,
        CancellationToken ct)
    {
        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = _options.SystemPrompt + "\n\nAPPROVED FAQ CONTEXT:\n" + context }
            };

            // Include last 6 messages of history for follow-up context
            if (history is not null && history.Count > 0)
            {
                foreach (var m in history.TakeLast(6))
                {
                    messages.Add(new { role = m.Role, content = m.Content });
                }
            }

            messages.Add(new { role = "user", content = question });

            var payload = new
            {
                model = _options.Model,
                messages,
                max_tokens = _options.MaxTokens,
                temperature = _options.Temperature,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);

            var client = _httpFactory.CreateClient("Grok");
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint.TrimEnd('/')}/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await client.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Grok API returned {Status}: {Body}", resp.StatusCode, body);
                return null;
            }

            var respJson = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(respJson);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content?.Trim();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Grok API timed out after {Timeout}s", _options.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grok API call failed");
            return null;
        }
    }
}