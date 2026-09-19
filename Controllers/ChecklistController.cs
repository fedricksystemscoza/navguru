using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.ViewModels;

namespace NavGuru.Controllers;

[Authorize]
public class ChecklistController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ChecklistController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var items = await _db.ChecklistItems
            .Where(c => c.UserId == user.Id)
            .OrderBy(c => c.Order)
            .ToListAsync();

        var vm = new ChecklistViewModel
        {
            Items = items
                .GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList()),
            Total = items.Count,
            Completed = items.Count(i => i.IsComplete)
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var item = await _db.ChecklistItems.FindAsync(id);
        if (item is null || item.UserId != user.Id) return NotFound();

        item.IsComplete = !item.IsComplete;
        await _db.SaveChangesAsync();

        var allItems = await _db.ChecklistItems
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        return Json(new
        {
            success = true,
            isComplete = item.IsComplete,
            completed = allItems.Count(i => i.IsComplete),
            total = allItems.Count,
            percent = allItems.Count == 0
                ? 0
                : (int)Math.Round((double)allItems.Count(i => i.IsComplete) / allItems.Count * 100)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] ChecklistAddRequest req)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.Title) || req.Title.Length > 200)
            return BadRequest();

        var maxOrder = await _db.ChecklistItems
            .Where(c => c.UserId == user.Id)
            .Select(c => (int?)c.Order)
            .MaxAsync() ?? 0;

        var item = new ChecklistItem
        {
            Title = req.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(req.Category) ? "General" : req.Category.Trim(),
            Order = maxOrder + 1,
            IsComplete = false,
            UserId = user.Id
        };

        _db.ChecklistItems.Add(item);
        await _db.SaveChangesAsync();

        return Json(new { success = true, id = item.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var item = await _db.ChecklistItems.FindAsync(id);
        if (item is null || item.UserId != user.Id) return NotFound();

        _db.ChecklistItems.Remove(item);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }
}