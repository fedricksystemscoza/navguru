using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.Service;
using NavGuru.Services;        // ← NEW
using NavGuru.ViewModels;

namespace NavGuru.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService _email;
    private readonly IQrCodeService _qr;

    public HomeController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IEmailService email,
        IQrCodeService qr)                     
    {
        _db = db;
        _users = users;
        _email = email; 
        _qr = qr;       
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        if (await _users.IsInRoleAsync(user, "Admin"))
            return RedirectToAction("Index", "Admin");

        var now = DateTime.Now;

        var upcoming = await _db.OrientationEvents
            .Where(e => e.EndTime >= now)
            .OrderBy(e => e.StartTime)
            .Take(5)
            .ToListAsync();

        var checklist = await _db.ChecklistItems
            .Where(c => c.UserId == user.Id)
            .OrderBy(c => c.Order)
            .ToListAsync();

        var recentNotifs = await _db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            FullName = user.FullName,
            StudentNumber = user.StudentNumber,
            Faculty = user.Faculty,
            NextEvent = upcoming.FirstOrDefault(),
            UpcomingEvents = upcoming,
            ChecklistTotal = checklist.Count,
            ChecklistCompleted = checklist.Count(c => c.IsComplete),
            RecentChecklist = checklist.Take(5).ToList(),
            FaqCount = await _db.FaqEntries.CountAsync(f => f.IsApproved),
            BadgesEarned = checklist.Count(c => c.IsComplete) / 3,
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead),
            RecentNotifications = recentNotifs,
            SupportResourceCount = await _db.SupportResources.CountAsync(r => r.IsActive)
        };

        return View(vm);
    }

    public async Task<IActionResult> Notifications()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var list = await _db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        // Mark all as read when opening the page
        foreach (var n in list.Where(n => !n.IsRead))
            n.IsRead = true;
        await _db.SaveChangesAsync();

        return View(list);
    }

    /// <summary>
    /// DEV ONLY — Sends a test email to the currently logged-in user.
    /// Delete before final submission.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> TestEmail()
    {
        var user = await _users.GetUserAsync(User);
        if (user?.Email is null)
            return Content("No logged-in user or missing email.");

        var html = $@"
        <div style='font-family:sans-serif;padding:24px;'>
            <h1 style='color:#0A1F44;'>NavBuddy email test ✅</h1>
            <p>Hi {(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName)},</p>
            <p>If you're reading this, your MailKit SMTP setup is working correctly.</p>
            <p style='color:#64748b;font-size:12px;'>Sent at {DateTime.Now:dd MMM yyyy HH:mm:ss}</p>
        </div>";

        try
        {
            await _email.SendAsync(user.Email, user.FullName ?? user.Email,
                "NavBuddy test email", html);
            return Content($"Sent to {user.Email} — check your inbox (and spam).");
        }
        catch (Exception ex)
        {
            return Content($"FAILED: {ex.GetType().Name} — {ex.Message}");
        }
    }

    [Authorize]
    public async Task<IActionResult> EventQr(int id)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var ev = await _db.OrientationEvents.FindAsync(id);
        if (ev is null) return NotFound();

        var existingCheckIn = await _db.CheckIns
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.OrientationEventId == id);

        var payload = _qr.GenerateCheckInPayload(user.Id, id);

        return View(new EventQrViewModel
        {
            Event = ev,
            UserId = user.Id,
            UserFullName = user.FullName ?? user.Email ?? "Student",
            QrPayload = payload,
            QrImageDataUri = _qr.GenerateQrPngDataUri(payload),
            AlreadyCheckedIn = existingCheckIn is not null,
            CheckedInAt = existingCheckIn?.CheckedInAt
        });
    }

    [Authorize]
    public async Task<IActionResult> MyQrCodes()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var now = DateTime.Now;

        // Upcoming events + today's events (still ongoing)
        var events = await _db.OrientationEvents
            .Where(e => e.EndTime >= now)
            .OrderBy(e => e.StartTime)
            .Take(10)
            .ToListAsync();

        // Get check-in status for each
        var checkIns = await _db.CheckIns
            .Where(c => c.UserId == user.Id)
            .ToDictionaryAsync(c => c.OrientationEventId, c => c.CheckedInAt);

        var vm = events.Select(ev =>
        {
            var payload = _qr.GenerateCheckInPayload(user.Id, ev.Id);
            return new EventQrCardViewModel
            {
                Event = ev,
                QrImageDataUri = _qr.GenerateQrPngDataUri(payload, 6),   // smaller QR for grid display
                QrPayload = payload,
                AlreadyCheckedIn = checkIns.ContainsKey(ev.Id),
                CheckedInAt = checkIns.ContainsKey(ev.Id) ? checkIns[ev.Id] : null
            };
        }).ToList();

        return View(vm);
    }

    public async Task<IActionResult> Support()
    {
        var list = await _db.SupportResources
            .Where(r => r.IsActive)
            .OrderBy(r => r.Category).ThenBy(r => r.Order)
            .ToListAsync();
        return View(list);
    }
    public async Task<IActionResult> CampusMap()
    {
        var locations = await _db.CampusLocations
            .Where(l => l.IsActive)
            .OrderBy(l => l.Order)
            .ToListAsync();
        return View(locations);
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    public IActionResult Error() => View();
}