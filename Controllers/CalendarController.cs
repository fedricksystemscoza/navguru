using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.ViewModels;

namespace NavGuru.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CalendarController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var now = DateTime.Now;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        if (m < 1) { m = 12; y--; }
        if (m > 12) { m = 1; y++; }

        var monthStart = new DateTime(y, m, 1);
        var monthEnd = monthStart.AddMonths(1);

        var events = await _db.OrientationEvents
            .Where(e => e.StartTime >= monthStart && e.StartTime < monthEnd)
            .OrderBy(e => e.StartTime)
            .ToListAsync();

        var user = await _users.GetUserAsync(User);
        var myCheckIns = user is null
            ? new List<int>()
            : await _db.CheckIns
                .Where(c => c.UserId == user.Id)
                .Select(c => c.OrientationEventId)
                .ToListAsync();

        return View(new CalendarViewModel
        {
            Year = y,
            Month = m,
            MonthName = monthStart.ToString("MMMM yyyy"),
            Events = events,
            MyCheckInEventIds = myCheckIns
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _db.OrientationEvents.FindAsync(id);
        if (ev is null) return NotFound();

        var user = await _users.GetUserAsync(User);
        var checkedIn = user is not null &&
            await _db.CheckIns.AnyAsync(c => c.UserId == user.Id && c.OrientationEventId == id);

        return Json(new
        {
            id = ev.Id,
            title = ev.Title,
            description = ev.Description,
            location = ev.Location,
            start = ev.StartTime.ToString("ddd dd MMM yyyy · HH:mm"),
            end = ev.EndTime.ToString("HH:mm"),
            isMandatory = ev.IsMandatory,
            checkedIn
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(int id)
    {
        var ev = await _db.OrientationEvents.FindAsync(id);
        if (ev is null) return NotFound();

        var ics = $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//NavGuru//EN
BEGIN:VEVENT
UID:{ev.Id}@navguru.local
DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}
DTSTART:{ev.StartTime.ToUniversalTime():yyyyMMddTHHmmssZ}
DTEND:{ev.EndTime.ToUniversalTime():yyyyMMddTHHmmssZ}
SUMMARY:{ev.Title}
DESCRIPTION:{ev.Description.Replace("\n", "\\n")}
LOCATION:{ev.Location}
END:VEVENT
END:VCALENDAR";

        var bytes = System.Text.Encoding.UTF8.GetBytes(ics);
        return File(bytes, "text/calendar", $"event-{ev.Id}.ics");
    }
}