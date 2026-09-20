using Ical.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.Services;
using NavGuru.ViewModels.Admin;

namespace NavGuru.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailService _email;
    private readonly IStudentNumberGenerator _studentNumberGen;     

    public AdminController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IServiceProvider serviceProvider,
        IEmailService email,
        IStudentNumberGenerator studentNumberGen)                    
    {
        _db = db;
        _users = users;
        _serviceProvider = serviceProvider;
        _email = email;
        _studentNumberGen = studentNumberGen;                        
    }


    public async Task<IActionResult> Index()
    {
        ViewBag.EventCount = await _db.OrientationEvents.CountAsync();
        ViewBag.FaqCount = await _db.FaqEntries.CountAsync();
        ViewBag.NotificationCount = await _db.Notifications.CountAsync();

        var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        ViewBag.StudentCount = studentRole is null ? 0 :
            await _db.UserRoles.CountAsync(ur => ur.RoleId == studentRole.Id);

        return View();
    }

    // =====================================================
    // ORIENTATIONS — List + CRUD
    // =====================================================
    public async Task<IActionResult> Orientations()
    {
        var events = await _db.OrientationEvents
            .OrderBy(e => e.StartTime)
            .ToListAsync();
        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        var vm = new EventFormViewModel
        {
            StartTime = DateTime.Today.AddDays(1).AddHours(9),
            EndTime = DateTime.Today.AddDays(1).AddHours(10)
        };
        return View("EventForm", vm);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel vm)
    {
        if (vm.EndTime <= vm.StartTime)
            ModelState.AddModelError(nameof(vm.EndTime), "End time must be after start time.");

        if (!ModelState.IsValid) return View("EventForm", vm);

        var ev = new OrientationEvent
        {
            Title = vm.Title,
            Description = vm.Description,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            Location = vm.Location,
            IsMandatory = vm.IsMandatory
        };

        _db.OrientationEvents.Add(ev);
        await _db.SaveChangesAsync();

        var eventId = ev.Id;
        // Capture the scope factory while the request is still alive
        var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Reload event in this scope
                var evReloaded = await db.OrientationEvents.FindAsync(eventId);
                if (evReloaded is null) return;

                var studentRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                if (studentRole is null) return;

                var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                var adminIds = adminRole is null
                    ? new List<string>()
                    : await db.UserRoles.Where(ur => ur.RoleId == adminRole.Id).Select(ur => ur.UserId).ToListAsync();

                var studentIds = await db.UserRoles
                    .Where(ur => ur.RoleId == studentRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var recipients = await db.Users
                    .Where(u => studentIds.Contains(u.Id)
                             && !adminIds.Contains(u.Id)
                             && u.EmailNotifications
                             && u.Email != null
                             && u.Email != "")
                    .ToListAsync();

                Console.WriteLine($"[NavGuru] Event '{evReloaded.Title}' — notifying {recipients.Count} student(s)");

                foreach (var s in recipients)
                {
                    try
                    {
                        var notification = new Notification
                        {
                            UserId = s.Id,
                            Title = $"New event: {evReloaded.Title}",
                            Message = $"{evReloaded.StartTime:ddd dd MMM · HH:mm} · {evReloaded.Location}",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Notifications.Add(notification);
                        await db.SaveChangesAsync();

                        if (!string.IsNullOrWhiteSpace(s.Email))
                        {
                            var html = EventEmailTemplates.NewEventHtml(evReloaded, s.FullName ?? "");
                            await email.SendAsync(s.Email, s.FullName ?? s.Email, $"New event: {evReloaded.Title}", html);
                        }

                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NavGuru] Notify failed for {s.Email}: {ex.Message}");
                    }
                }

                Console.WriteLine($"[NavGuru] Done notifying for event '{evReloaded.Title}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NavGuru] Background notification task crashed: {ex}");
            }
        });

        TempData["Success"] = $"Event \"{ev.Title}\" created. Students are being notified.";

        return RedirectToAction(nameof(Orientations));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await _db.OrientationEvents.FindAsync(id);
        if (ev is null) return NotFound();

        return View("EventForm", new EventFormViewModel
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            Location = ev.Location,
            IsMandatory = ev.IsMandatory
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormViewModel vm)
    {
        if (vm.EndTime <= vm.StartTime)
            ModelState.AddModelError(nameof(vm.EndTime), "End time must be after start time.");

        if (!ModelState.IsValid) return View("EventForm", vm);

        var ev = await _db.OrientationEvents.FindAsync(vm.Id);
        if (ev is null) return NotFound();

        ev.Title = vm.Title;
        ev.Description = vm.Description;
        ev.StartTime = vm.StartTime;
        ev.EndTime = vm.EndTime;
        ev.Location = vm.Location;
        ev.IsMandatory = vm.IsMandatory;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Event \"{ev.Title}\" updated.";
        return RedirectToAction(nameof(Orientations));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.OrientationEvents.FindAsync(id);
        if (ev is null) return NotFound();

        _db.OrientationEvents.Remove(ev);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Event \"{ev.Title}\" deleted.";
        return RedirectToAction(nameof(Orientations));
    }

    // =====================================================
    // FAQS — List + CRUD
    // =====================================================
    public async Task<IActionResult> Faqs()
    {
        var faqs = await _db.FaqEntries
            .OrderBy(f => f.Category).ThenBy(f => f.Question)
            .ToListAsync();
        return View(faqs);
    }

    [HttpGet]
    public IActionResult CreateFaq() => View("FaqForm", new FaqFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaq(FaqFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("FaqForm", vm);

        _db.FaqEntries.Add(new FaqEntry
        {
            Question = vm.Question,
            Answer = vm.Answer,
            Category = vm.Category,
            IsApproved = vm.IsApproved
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "FAQ created.";
        return RedirectToAction(nameof(Faqs));
    }

    [HttpGet]
    public async Task<IActionResult> EditFaq(int id)
    {
        var faq = await _db.FaqEntries.FindAsync(id);
        if (faq is null) return NotFound();

        return View("FaqForm", new FaqFormViewModel
        {
            Id = faq.Id,
            Question = faq.Question,
            Answer = faq.Answer,
            Category = faq.Category,
            IsApproved = faq.IsApproved
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFaq(FaqFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("FaqForm", vm);

        var faq = await _db.FaqEntries.FindAsync(vm.Id);
        if (faq is null) return NotFound();

        faq.Question = vm.Question;
        faq.Answer = vm.Answer;
        faq.Category = vm.Category;
        faq.IsApproved = vm.IsApproved;

        await _db.SaveChangesAsync();

        TempData["Success"] = "FAQ updated.";
        return RedirectToAction(nameof(Faqs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFaq(int id)
    {
        var faq = await _db.FaqEntries.FindAsync(id);
        if (faq is null) return NotFound();

        _db.FaqEntries.Remove(faq);
        await _db.SaveChangesAsync();

        TempData["Success"] = "FAQ deleted.";
        return RedirectToAction(nameof(Faqs));
    }

    // =====================================================
    // STUDENTS — List + CRUD
    // =====================================================
    public async Task<IActionResult> Students()
    {
        var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

        var allIds = await _db.UserRoles
            .Where(ur => (studentRole != null && ur.RoleId == studentRole.Id) ||
                         (adminRole != null && ur.RoleId == adminRole.Id))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _db.Users
            .Where(u => allIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var adminIds = adminRole is null
            ? new List<string>()
            : await _db.UserRoles.Where(ur => ur.RoleId == adminRole.Id)
                                 .Select(ur => ur.UserId).ToListAsync();

        var vm = users.Select(u => new ViewModels.AdminStudentViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? "",
            StudentNumber = u.StudentNumber,
            Faculty = u.Faculty,
            CreatedAt = u.CreatedAt,
            IsAdmin = adminIds.Contains(u.Id)
        }).ToList();

        return View(vm);


    }

    [HttpGet]
    public IActionResult CreateStudent() => View("StudentForm", new StudentFormViewModel());


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(StudentFormViewModel vm)
    {
        // Validate password presence
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Password is required when creating a new user.");

        // Validate email uniqueness
        if (!string.IsNullOrWhiteSpace(vm.Email))
        {
            var existing = await _users.FindByEmailAsync(vm.Email);
            if (existing is not null)
                ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
        }

        // If validation failed, redisplay the form
        if (!ModelState.IsValid)
            return View("StudentForm", vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            EmailConfirmed = true,
            FullName = vm.FullName,
            StudentNumber = vm.IsAdmin
         ? null                                                   
         : await _studentNumberGen.GenerateAsync(),               
            Faculty = vm.Faculty
        };
        var result = await _users.CreateAsync(user, vm.Password!);

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View("StudentForm", vm);
        }

        await _users.AddToRoleAsync(user, vm.IsAdmin ? "Admin" : "Student");

        // Auto-seed default checklist for new students
        if (!vm.IsAdmin)
            await SeedChecklistForAsync(user.Id);

        TempData["Success"] = $"Account for {vm.FullName} created.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet]
    public async Task<IActionResult> EditStudent(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var isAdmin = await _users.IsInRoleAsync(user, "Admin");

        return View("StudentForm", new StudentFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            StudentNumber = user.StudentNumber,
            Faculty = user.Faculty,
            IsAdmin = isAdmin
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(StudentFormViewModel vm)
    {
        if (string.IsNullOrEmpty(vm.Id)) return NotFound();

        var user = await _users.FindByIdAsync(vm.Id);
        if (user is null) return NotFound();

        if (!ModelState.IsValid) return View("StudentForm", vm);

        user.FullName = vm.FullName;
        user.Email = vm.Email;
        user.UserName = vm.Email;
        user.StudentNumber = vm.StudentNumber;
        user.Faculty = vm.Faculty;

        await _users.UpdateAsync(user);

        // Handle role change
        var wasAdmin = await _users.IsInRoleAsync(user, "Admin");
        if (vm.IsAdmin && !wasAdmin)
        {
            await _users.RemoveFromRoleAsync(user, "Student");
            await _users.AddToRoleAsync(user, "Admin");
        }
        else if (!vm.IsAdmin && wasAdmin)
        {
            await _users.RemoveFromRoleAsync(user, "Admin");
            await _users.AddToRoleAsync(user, "Student");
        }

        // Optional password reset
        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            await _users.ResetPasswordAsync(user, token, vm.Password);
        }

        TempData["Success"] = $"{user.FullName}'s account updated.";
        return RedirectToAction(nameof(Students));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudent(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Prevent deleting yourself
        var currentId = _users.GetUserId(User);
        if (user.Id == currentId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Students));
        }

        // Clean up their data
        _db.ChecklistItems.RemoveRange(_db.ChecklistItems.Where(c => c.UserId == id));
        _db.CheckIns.RemoveRange(_db.CheckIns.Where(c => c.UserId == id));
        _db.Notifications.RemoveRange(_db.Notifications.Where(n => n.UserId == id));
        await _db.SaveChangesAsync();

        await _users.DeleteAsync(user);

        TempData["Success"] = $"Account for {user.FullName} deleted.";
        return RedirectToAction(nameof(Students));
    }

    // =====================================================
    // NOTIFICATIONS — List + CRUD
    // =====================================================
    public async Task<IActionResult> Notifications()
    {
        var list = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateNotification() => View("NotificationForm", new NotificationFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNotification(NotificationFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("NotificationForm", vm);

        var now = DateTime.UtcNow;

        if (vm.Broadcast)
        {
            // Send to all students
            var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            if (studentRole is not null)
            {
                var studentIds = await _db.UserRoles
                    .Where(ur => ur.RoleId == studentRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var sid in studentIds)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = sid,
                        Title = vm.Title,
                        Message = vm.Message,
                        IsRead = false,
                        CreatedAt = now
                    });
                }
            }
        }
        else
        {
            // Send to admin only (self) as placeholder — target selector is a future feature
            var adminId = _users.GetUserId(User)!;
            _db.Notifications.Add(new Notification
            {
                UserId = adminId,
                Title = vm.Title,
                Message = vm.Message,
                IsRead = false,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = vm.Broadcast
            ? "Notification broadcast to all students."
            : "Notification created.";
        return RedirectToAction(nameof(Notifications));
    }

    //[HttpPost, ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteNotificationGroup(int id)
    //{
    //    var sample = await _db.Notifications.FindAsync(id);
    //    if (sample is null) return NotFound();

    //    // Delete all notifications with matching title, message, and creation time
    //    var toDelete = await _db.Notifications
    //        .Where(n => n.Title == sample.Title
    //                 && n.Message == sample.Message
    //                 && n.CreatedAt == sample.CreatedAt)
    //        .ToListAsync();

    //    _db.Notifications.RemoveRange(toDelete);
    //    await _db.SaveChangesAsync();

    //    TempData["Success"] = $"Deleted {toDelete.Count} notification(s).";
    //    return RedirectToAction(nameof(Notifications));
    //}

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n is null) return NotFound();

        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Notification deleted.";
        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotificationGroup(int id)
    {
        var sample = await _db.Notifications.FindAsync(id);
        if (sample is null) return NotFound();

        var toDelete = await _db.Notifications
            .Where(n => n.Title == sample.Title
                     && n.Message == sample.Message
                     && n.CreatedAt == sample.CreatedAt)
            .ToListAsync();

        _db.Notifications.RemoveRange(toDelete);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Deleted {toDelete.Count} notification(s).";
        return RedirectToAction(nameof(Notifications));
    }
    // =====================================================
    // CALENDAR (same events, different framing)
    // =====================================================
    public async Task<IActionResult> Calendar()
    {
        var events = await _db.OrientationEvents
            .OrderBy(e => e.StartTime)
            .ToListAsync();

        ViewBag.EventCount = events.Count;
        return View(events);
    }

    // =====================================================
    // SUPPORT RESOURCES — List + CRUD
    // =====================================================
    public async Task<IActionResult> Resources()
    {
        var list = await _db.SupportResources
            .OrderBy(r => r.Category).ThenBy(r => r.Order)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateResource() => View("ResourceForm", new ResourceFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResource(ResourceFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("ResourceForm", vm);

        _db.SupportResources.Add(new SupportResource
        {
            Title = vm.Title,
            Description = vm.Description,
            Category = vm.Category,
            Url = vm.Url,
            ContactEmail = vm.ContactEmail,
            ContactPhone = vm.ContactPhone,
            Location = vm.Location,
            Order = vm.Order,
            IsActive = vm.IsActive
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Resource created.";
        return RedirectToAction(nameof(Resources));
    }

    [HttpGet]
    public async Task<IActionResult> EditResource(int id)
    {
        var r = await _db.SupportResources.FindAsync(id);
        if (r is null) return NotFound();

        return View("ResourceForm", new ResourceFormViewModel
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Category = r.Category,
            Url = r.Url,
            ContactEmail = r.ContactEmail,
            ContactPhone = r.ContactPhone,
            Location = r.Location,
            Order = r.Order,
            IsActive = r.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditResource(ResourceFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("ResourceForm", vm);

        var r = await _db.SupportResources.FindAsync(vm.Id);
        if (r is null) return NotFound();

        r.Title = vm.Title;
        r.Description = vm.Description;
        r.Category = vm.Category;
        r.Url = vm.Url;
        r.ContactEmail = vm.ContactEmail;
        r.ContactPhone = vm.ContactPhone;
        r.Location = vm.Location;
        r.Order = vm.Order;
        r.IsActive = vm.IsActive;
        r.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Resource updated.";
        return RedirectToAction(nameof(Resources));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var r = await _db.SupportResources.FindAsync(id);
        if (r is null) return NotFound();

        _db.SupportResources.Remove(r);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Resource deleted.";
        return RedirectToAction(nameof(Resources));
    }

    // =====================================================
    // HELPERS
    // =====================================================
    private async Task SeedChecklistForAsync(string userId)
    {
        var defaults = new[]
        {
            "Activate your student email account",
            "Connect to campus Wi-Fi",
            "Collect your student card",
            "Register for your modules",
            "Use the NavBuddy mobile web app",
            "Complete library orientation",
            "Join the student portal",
            "Attend the welcome address"
        };

        var i = 1;
        foreach (var title in defaults)
        {
            _db.ChecklistItems.Add(new ChecklistItem
            {
                Title = title,
                Category = "General",
                Order = i++,
                IsComplete = false,
                UserId = userId
            });
        }
        await _db.SaveChangesAsync();
    }

    // =====================================================
    // CAMPUS MAP — List + CRUD
    // =====================================================
    public async Task<IActionResult> CampusMap()
    {
        var list = await _db.CampusLocations
            .OrderBy(l => l.Category).ThenBy(l => l.Order)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateLocation() => View("LocationForm", new CampusLocationFormViewModel
    {
        Latitude = -33.9608,
        Longitude = 25.6022
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(CampusLocationFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("LocationForm", vm);

        _db.CampusLocations.Add(new CampusLocation
        {
            Name = vm.Name,
            Description = vm.Description,
            Category = vm.Category,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            Address = vm.Address,
            OpeningHours = vm.OpeningHours,
            ContactPhone = vm.ContactPhone,
            IconName = vm.IconName,
            Order = vm.Order,
            IsActive = vm.IsActive
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Location \"{vm.Name}\" added.";
        return RedirectToAction(nameof(CampusMap));
    }

    [HttpGet]
    public async Task<IActionResult> EditLocation(int id)
    {
        var l = await _db.CampusLocations.FindAsync(id);
        if (l is null) return NotFound();

        return View("LocationForm", new CampusLocationFormViewModel
        {
            Id = l.Id,
            Name = l.Name,
            Description = l.Description,
            Category = l.Category,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Address = l.Address,
            OpeningHours = l.OpeningHours,
            ContactPhone = l.ContactPhone,
            IconName = l.IconName,
            Order = l.Order,
            IsActive = l.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLocation(CampusLocationFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("LocationForm", vm);

        var l = await _db.CampusLocations.FindAsync(vm.Id);
        if (l is null) return NotFound();

        l.Name = vm.Name;
        l.Description = vm.Description;
        l.Category = vm.Category;
        l.Latitude = vm.Latitude;
        l.Longitude = vm.Longitude;
        l.Address = vm.Address;
        l.OpeningHours = vm.OpeningHours;
        l.ContactPhone = vm.ContactPhone;
        l.IconName = vm.IconName;
        l.Order = vm.Order;
        l.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Location \"{l.Name}\" updated.";
        return RedirectToAction(nameof(CampusMap));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var l = await _db.CampusLocations.FindAsync(id);
        if (l is null) return NotFound();

        _db.CampusLocations.Remove(l);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Location \"{l.Name}\" deleted.";
        return RedirectToAction(nameof(CampusMap));
    }

    private async Task NotifyStudentsOfNewEventAsync(int eventId)
    {
        try
        {
            // Background task = new scope, fresh DbContext + services
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var ev = await db.OrientationEvents.FindAsync(eventId);
            if (ev is null)
            {
                Console.WriteLine($"[NavGuru] Event #{eventId} not found — cannot notify.");
                return;
            }

            // Find the Student role
            var studentRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            if (studentRole is null)
            {
                Console.WriteLine("[NavGuru] No 'Student' role found — skipping notifications.");
                return;
            }

            // IDs of all students
            var studentIds = await db.UserRoles
                .Where(ur => ur.RoleId == studentRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // IDs of all admins (to exclude, in case of overlap)
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminIds = adminRole is null
                ? new List<string>()
                : await db.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

            // Students only, opted-in, valid email, not admin
            var recipients = await db.Users
                .Where(u => studentIds.Contains(u.Id)
                         && !adminIds.Contains(u.Id)
                         && u.EmailNotifications
                         && u.Email != null
                         && u.Email != "")
                .ToListAsync();

            Console.WriteLine($"[NavGuru] Event '{ev.Title}' — notifying {recipients.Count} student(s)");

            var sent = 0;
            var failed = 0;

            foreach (var s in recipients)
            {
                if (string.IsNullOrWhiteSpace(s.Email)) continue;

                try
                {
                    // Personalized HTML
                    var html = EventEmailTemplates.NewEventHtml(ev, s.FullName ?? "");

                    await email.SendAsync(
                        s.Email!,
                        s.FullName ?? s.Email!,
                        $"New event: {ev.Title}",
                        html);

                    // In-app notification (bell in nav bar)
                    db.Notifications.Add(new Notification
                    {
                        UserId = s.Id,
                        Title = $"New event: {ev.Title}",
                        Message = $"{ev.StartTime:ddd dd MMM · HH:mm} · {ev.Location}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    sent++;

                    // Throttle — Gmail free tier is ~100/day, ~1/sec
                    await Task.Delay(150);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"[NavGuru] Failed to notify {s.Email}: {ex.Message}");
                }
            }

            await db.SaveChangesAsync();

            Console.WriteLine($"[NavGuru] Event notification complete: {sent} sent, {failed} failed (of {recipients.Count})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavGuru] Event notification task crashed: {ex}");
        }
    }
}   // ← this must be the closing brace of AdminController

    // =====================================================
    //