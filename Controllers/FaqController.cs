using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.Services;
using NavGuru.ViewModels;

namespace NavGuru.Controllers;

[Authorize]
public class FaqController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFaqAssistantService _assistant;
    private readonly UserManager<ApplicationUser> _users;

    public FaqController(
        ApplicationDbContext db,
        IFaqAssistantService assistant,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _assistant = assistant;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var faqs = await _db.FaqEntries
            .Where(f => f.IsApproved)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Question)
            .ToListAsync();

        var vm = new FaqPageViewModel
        {
            Faqs = faqs,
            Categories = faqs.Select(f => f.Category).Distinct().OrderBy(c => c).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] FaqAskRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Question))
            return Json(new { success = false, message = "Please type a question." });

        var result = await _assistant.AskAsync(req.Question, req.History);

        return Json(new
        {
            success = true,
            answer = result.Answer,
            usedAi = result.UsedAi,
            fromCache = result.FromCache,
            fallbackReason = result.FallbackReason,
            sources = result.SourceFaqIds
        });
    }

    public class FaqAskRequest
    {
        public string? Question { get; set; }
        public List<FaqChatMessage>? History { get; set; }
    }
}