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
public class CheckInController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IQrCodeService _qr;
    public CheckInController(ApplicationDbContext db,
                             UserManager<ApplicationUser> users,
                             IQrCodeService qr)
    {
        _db = db;
        _users = users;
        _qr = qr;
    }

    // =====================================================
    // STUDENT — Scanner page
    // =====================================================
    [HttpGet]
    public IActionResult Scan()
    {
        return View();
    }

    // =====================================================
    // STUDENT — API endpoint hit by the scanner after decoding
    // =====================================================
    [HttpPost]
    [IgnoreAntiforgeryToken]   // we validate via token, not cookie (see note below)
    public async Task<IActionResult> Submit([FromBody] CheckInSubmitRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Payload))
            return Json(new { success = false, message = "No QR payload received." });

        // Parse payload: "navguru://checkin?event=3&token=abc..."
        var ev = ParsePayload(req.Payload);
        if (ev is null)
            return Json(new { success = false, message = "This doesn't look like a NavBuddy event QR." });

        var (eventId, token) = ev.Value;

        var orientationEvent = await _db.OrientationEvents.FindAsync(eventId);
        if (orientationEvent is null)
            return Json(new { success = false, message = "Event not found." });

        if (orientationEvent.CheckInToken != token)
            return Json(new { success = false, message = "Invalid QR code." });

        // Check time window — allow check-in from 15 min before start to 30 min after end
        var now = DateTime.Now;
        var windowStart = orientationEvent.StartTime.AddMinutes(-15);
        var windowEnd = orientationEvent.EndTime.AddMinutes(30);

        if (now < windowStart)
            return Json(new { success = false, message = $"Check-in opens at {windowStart:HH:mm}." });
        if (now > windowEnd)
            return Json(new { success = false, message = "Check-in window has closed." });

        var user = await _users.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "Session expired. Please log in again." });

        // Already checked in?
        var existing = await _db.CheckIns
            .FirstOrDefaultAsync(c => c.UserId == user.Id
                                   && c.OrientationEventId == eventId);

        if (existing is not null)
        {
            return Json(new
            {
                success = true,
                alreadyCheckedIn = true,
                eventTitle = orientationEvent.Title,
                message = $"You already checked in at {existing.CheckedInAt:HH:mm}."
            });
        }

        _db.CheckIns.Add(new CheckIn
        {
            UserId = user.Id,
            OrientationEventId = eventId,
            CheckedInAt = DateTime.UtcNow,
            QrPayload = req.Payload
        });
        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            alreadyCheckedIn = false,
            eventTitle = orientationEvent.Title,
            eventLocation = orientationEvent.Location,
            message = $"Checked in to {orientationEvent.Title}."
        });
    }

    // =====================================================
    // STUDENT — Manual code fallback (in case camera fails)
    // =====================================================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Enter a code.";
            return RedirectToAction(nameof(Scan));
        }

        var orientationEvent = await _db.OrientationEvents
            .FirstOrDefaultAsync(e => e.CheckInToken == code.Trim());

        if (orientationEvent is null)
        {
            TempData["Error"] = "Invalid code.";
            return RedirectToAction(nameof(Scan));
        }

        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var exists = await _db.CheckIns
            .AnyAsync(c => c.UserId == user.Id && c.OrientationEventId == orientationEvent.Id);

        if (!exists)
        {
            _db.CheckIns.Add(new CheckIn
            {
                UserId = user.Id,
                OrientationEventId = orientationEvent.Id,
                CheckedInAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"Checked in to {orientationEvent.Title}.";
        return RedirectToAction(nameof(Scan));
    }

    // =====================================================
    // STUDENT — My check-in history
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var vm = await (
            from c in _db.CheckIns
            join e in _db.OrientationEvents on c.OrientationEventId equals e.Id
            where c.UserId == user.Id
            orderby c.CheckedInAt descending
            select new CheckInHistoryRow
            {
                EventTitle = e.Title,
                Location = e.Location,
                CheckedInAt = c.CheckedInAt,
                IsMandatory = e.IsMandatory
            }).ToListAsync();

        return View(vm);
    }

    // =====================================================
    // ADMIN — Display the QR for an event
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Display(int id)
    {
        var orientationEvent = await _db.OrientationEvents.FindAsync(id);
        if (orientationEvent is null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var payload = $"navguru://checkin?event={orientationEvent.Id}&token={orientationEvent.CheckInToken}";

        var vm = new QrDisplayViewModel
        {
            EventId = orientationEvent.Id,
            EventTitle = orientationEvent.Title,
            EventLocation = orientationEvent.Location,
            EventStart = orientationEvent.StartTime,
            EventEnd = orientationEvent.EndTime,
            Payload = payload,
            QrImage = _qr.GenerateQrPngDataUri(payload),
            ManualCode = orientationEvent.CheckInToken,
            CheckedInCount = await _db.CheckIns.CountAsync(c => c.OrientationEventId == id)
        };

        return View(vm);
    }

    // =====================================================
    // ADMIN — Live check-ins for an event (JSON, polled by Display view)
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> LiveCount(int id)
    {
        var count = await _db.CheckIns.CountAsync(c => c.OrientationEventId == id);
        var recent = await (
            from c in _db.CheckIns
            join u in _db.Users on c.UserId equals u.Id
            where c.OrientationEventId == id
            orderby c.CheckedInAt descending
            select new { name = u.FullName, at = c.CheckedInAt }
        ).Take(5).ToListAsync();

        return Json(new { count, recent });
    }

    // =====================================================
    // HELPERS
    // =====================================================
    private static (int eventId, string token)? ParsePayload(string payload)
    {
        try
        {
            if (!payload.StartsWith("navguru://checkin?")) return null;

            var query = payload.Substring("navguru://checkin?".Length);
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

            int? eventId = null;
            string? token = null;

            foreach (var p in parts)
            {
                var kv = p.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "event" && int.TryParse(kv[1], out var id)) eventId = id;
                if (kv[0] == "token") token = kv[1];
            }

            if (eventId is null || string.IsNullOrWhiteSpace(token)) return null;
            return (eventId.Value, token);
        }
        catch
        {
            return null;
        }
    }
}