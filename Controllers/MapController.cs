using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;

namespace NavGuru.Controllers;

[Authorize]
public class MapController : Controller
{
    private readonly ApplicationDbContext _db;

    public MapController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Return resources that have coordinates
        var locations = await _db.SupportResources
            .Where(r => r.IsActive && r.Latitude != null && r.Longitude != null)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Order)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.Category,
                r.Location,
                r.ContactEmail,
                r.ContactPhone,
                r.Latitude,
                r.Longitude
            })
            .ToListAsync();

        ViewBag.LocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);
        return View();
    }
}