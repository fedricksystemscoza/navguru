using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NavGuru.Models;
using NavGuru.Services;

namespace NavGuru.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var gen = sp.GetRequiredService<IStudentNumberGenerator>();

        // ---- Roles ----
        foreach (var role in new[] { "Student", "Admin" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));


        var student = await users.FindByEmailAsync("alex.smith@navguru.edu");
        if (student is null)
        {
            student = new ApplicationUser
            {
                UserName = "alex.smith@navguru.edu",
                Email = "alex.smith@navguru.edu",
                EmailConfirmed = true,
                FullName = "Tshilidzi",
                StudentNumber = await gen.GenerateAsync(),
                Faculty = "Science"
            };
            await users.CreateAsync(student, "Passw0rd!");
            await users.AddToRoleAsync(student, "Student");
        }

        // ---- Demo Admin ----
        var admin = await users.FindByEmailAsync("sarah.jenkins@navguru.edu");
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = "sarah.jenkins@navguru.edu",
                Email = "sarah.jenkins@navguru.edu",
                EmailConfirmed = true,
                FullName = "Sarah Jenkins"
            };
            await users.CreateAsync(admin, "Passw0rd!");
            await users.AddToRoleAsync(admin, "Admin");
        }
        // ---- Optional demo check-ins ----
        if (!await db.CheckIns.AnyAsync() && student is not null)
        {
            var firstEvent = await db.OrientationEvents
                .OrderBy(e => e.StartTime)
                .FirstOrDefaultAsync();

            if (firstEvent is not null)
            {
                db.CheckIns.Add(new CheckIn
                {
                    UserId = student.Id,
                    OrientationEventId = firstEvent.Id,
                    CheckedInAt = DateTime.UtcNow.AddMinutes(-15)
                });
                await db.SaveChangesAsync();
            }
        }
    


        // ---- Orientation Events ----
        if (!await db.OrientationEvents.AnyAsync())
        {
            var today = DateTime.Today;
            db.OrientationEvents.AddRange(
                new OrientationEvent
                {
                    Title = "Welcome Address by the Vice-Chancellor",
                    Description = "Official opening of orientation week. Attendance is mandatory for all first-year students.",
                    StartTime = today.AddDays(1).AddHours(9),
                    EndTime = today.AddDays(1).AddHours(10).AddMinutes(30),
                    Location = "Great Hall, South Campus",
                    IsMandatory = true
                },
                new OrientationEvent
                {
                    Title = "Campus Orientation Tour",
                    Description = "Guided walking tour of lecture halls, libraries, cafeterias, and student services.",
                    StartTime = today.AddDays(1).AddHours(11),
                    EndTime = today.AddDays(1).AddHours(12).AddMinutes(30),
                    Location = "Meet at South Campus Main Gate",
                    IsMandatory = false
                },
                new OrientationEvent
                {
                    Title = "Faculty of Science Meet & Greet",
                    Description = "Meet your lecturers, lab coordinators, and fellow students in your faculty.",
                    StartTime = today.AddDays(2).AddHours(10),
                    EndTime = today.AddDays(2).AddHours(12),
                    Location = "Science Building, Room S204",
                    IsMandatory = true
                },
                new OrientationEvent
                {
                    Title = "Student Card Collection",
                    Description = "Collect your official student card. Bring your ID/passport.",
                    StartTime = today.AddDays(2).AddHours(14),
                    EndTime = today.AddDays(2).AddHours(16),
                    Location = "Student Services, Building 12",
                    IsMandatory = true
                },
                new OrientationEvent
                {
                    Title = "Library & IT Orientation",
                    Description = "Learn how to use the library, Wi-Fi, student portal, and email.",
                    StartTime = today.AddDays(3).AddHours(9),
                    EndTime = today.AddDays(3).AddHours(10).AddMinutes(30),
                    Location = "Main Library, Level 1",
                    IsMandatory = false
                }
            );
            await db.SaveChangesAsync();
        }

        // ---- Checklist Items for the student ----
        if (student is not null && !await db.ChecklistItems.AnyAsync(c => c.UserId == student.Id))
        {
            db.ChecklistItems.AddRange(
                new ChecklistItem { Title = "Activate your student email account", Category = "IT", Order = 1, IsComplete = true, UserId = student.Id },
                new ChecklistItem { Title = "Connect to campus Wi-Fi", Category = "IT", Order = 2, IsComplete = true, UserId = student.Id },
                new ChecklistItem { Title = "Collect your student card", Category = "Admin", Order = 3, IsComplete = false, UserId = student.Id },
                new ChecklistItem { Title = "Register for your modules", Category = "Academic", Order = 4, IsComplete = true, UserId = student.Id },
                new ChecklistItem { Title = "Use the NavBuddy mobile web app", Category = "IT", Order = 5, IsComplete = false, UserId = student.Id },
                new ChecklistItem { Title = "Complete library orientation", Category = "Academic", Order = 6, IsComplete = false, UserId = student.Id },
                new ChecklistItem { Title = "Join the student portal", Category = "IT", Order = 7, IsComplete = true, UserId = student.Id },
                new ChecklistItem { Title = "Attend the welcome address", Category = "Social", Order = 8, IsComplete = false, UserId = student.Id }
            );
            await db.SaveChangesAsync();
        }

        // ---- FAQ Entries ----
        if (!await db.FaqEntries.AnyAsync())
        {
            db.FaqEntries.AddRange(
                new FaqEntry { Question = "Where do I get my student card?", Answer = "At the Student Services desk in Building 12, South Campus. Bring your ID or passport.", Category = "Admin", IsApproved = true },
                new FaqEntry { Question = "Is there Wi-Fi on campus?", Answer = "Yes — connect to 'NMU-Student' using your student number and portal password.", Category = "IT", IsApproved = true },
                new FaqEntry { Question = "How do I register for modules?", Answer = "Log into the student portal at portal.mandela.ac.za, go to Registration, and follow the prompts. Academic advisors are available in Building 12.", Category = "Academic", IsApproved = true },
                new FaqEntry { Question = "Where can I get food on campus?", Answer = "The main cafeteria is in the Student Centre, open 7am–7pm. There are also coffee shops in the library and Science Building.", Category = "Campus", IsApproved = true },
                new FaqEntry { Question = "What if I get lost on campus?", Answer = "Use the NavBuddy Campus Map feature, or ask any staff member — they're here to help.", Category = "Campus", IsApproved = true },
                new FaqEntry { Question = "How do I contact student support?", Answer = "Email support@mandela.ac.za or visit the Student Services desk, Building 12, weekdays 8am–4:30pm.", Category = "Support", IsApproved = true }
            );
            await db.SaveChangesAsync();
        }

        // ---- Support Resources ----
        if (!await db.SupportResources.AnyAsync())
        {
            db.SupportResources.AddRange(
                new SupportResource
                {
                    Title = "Student Counselling Services",
                    Description = "Free, confidential counselling for academic, personal, and emotional support. Walk-ins welcome.",
                    Category = "Mental Health",
                    ContactEmail = "counselling@mandela.ac.za",
                    ContactPhone = "+27 41 504 2922",
                    Location = "Building 12, South Campus",
                    Order = 1,
                    IsActive = true
                },
                new SupportResource
                {
                    Title = "Academic Advising",
                    Description = "Get help choosing modules, planning your degree, or improving your study skills.",
                    Category = "Academic",
                    ContactEmail = "advising@mandela.ac.za",
                    Location = "Faculty Offices",
                    Order = 2,
                    IsActive = true
                },
                new SupportResource
                {
                    Title = "IT Help Desk",
                    Description = "Wi-Fi, email, portal, and password issues. Walk in or call.",
                    Category = "IT",
                    ContactEmail = "ithelp@mandela.ac.za",
                    ContactPhone = "+27 41 504 2222",
                    Location = "Main Library, Level 1",
                    Order = 3,
                    IsActive = true
                },
                new SupportResource
                {
                    Title = "Financial Aid Office",
                    Description = "NSFAS, bursaries, fee queries, and payment plans.",
                    Category = "Finance",
                    ContactEmail = "finaid@mandela.ac.za",
                    Location = "Building 24, South Campus",
                    Order = 4,
                    IsActive = true
                },
                new SupportResource
                {
                    Title = "Campus Safety",
                    Description = "24/7 emergency line for on-campus safety concerns.",
                    Category = "Safety",
                    ContactPhone = "+27 41 504 1111",
                    Location = "Security Office, Main Gate",
                    Order = 5,
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
        }

        // ---- Welcome Notifications ----
        if (student is not null && !await db.Notifications.AnyAsync(n => n.UserId == student.Id))
        {
            db.Notifications.AddRange(
                new Notification
                {
                    UserId = student.Id,
                    Title = "Welcome to NavBuddy!",
                    Message = "Your orientation portal is ready. Complete your checklist to be fully set up for the semester.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new Notification
                {
                    UserId = student.Id,
                    Title = "Welcome Address Tomorrow",
                    Message = "Don't forget — the Vice-Chancellor's Welcome Address is tomorrow at 9:00 in the Great Hall. Attendance is mandatory.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            );
            await db.SaveChangesAsync();
        }

        // ---- Campus Locations ----
        if (!await db.CampusLocations.AnyAsync())
        {
            db.CampusLocations.AddRange(
                new CampusLocation
                {
                    Name = "Great Hall",
                    Description = "Main ceremonial venue. Home to graduations, welcomes, and major events.",
                    Category = "Landmark",
                    Latitude = -33.9603,
                    Longitude = 25.6055,
                    Address = "South Campus, University Way",
                    OpeningHours = "Mon–Fri · 08:00–17:00",
                    IconName = "fa-landmark",
                    Order = 1
                },
                new CampusLocation
                {
                    Name = "Student Services (Building 12)",
                    Description = "Student cards, fees, admissions queries, and general support.",
                    Category = "Service",
                    Latitude = -33.9615,
                    Longitude = 25.6045,
                    Address = "South Campus, Building 12",
                    OpeningHours = "Mon–Fri · 08:00–16:30",
                    ContactPhone = "+27 41 504 1111",
                    IconName = "fa-circle-info",
                    Order = 2
                },
                new CampusLocation
                {
                    Name = "Main Library",
                    Description = "Central library with study spaces, printing, and IT help desk.",
                    Category = "Service",
                    Latitude = -33.9598,
                    Longitude = 25.6042,
                    Address = "South Campus, next to the Great Hall",
                    OpeningHours = "Mon–Sun · 07:00–23:00",
                    IconName = "fa-book",
                    Order = 3
                },
                new CampusLocation
                {
                    Name = "Student Cafeteria",
                    Description = "Main dining hall with hot meals, coffee, and grab-and-go.",
                    Category = "Food",
                    Latitude = -33.9610,
                    Longitude = 25.6060,
                    Address = "Student Centre, Ground Floor",
                    OpeningHours = "Mon–Fri · 07:00–19:00",
                    IconName = "fa-utensils",
                    Order = 4
                },
                new CampusLocation
                {
                    Name = "Science Building (S Block)",
                    Description = "Lecture halls, labs, and the Faculty of Science offices.",
                    Category = "Building",
                    Latitude = -33.9608,
                    Longitude = 25.6075,
                    Address = "North Campus",
                    IconName = "fa-flask",
                    Order = 5
                },
                new CampusLocation
                {
                    Name = "Campus Security Office",
                    Description = "24/7 emergency line and safety services.",
                    Category = "Emergency",
                    Latitude = -33.9623,
                    Longitude = 25.6030,
                    Address = "Main Gate, South Campus",
                    OpeningHours = "24/7",
                    ContactPhone = "+27 41 504 1111",
                    IconName = "fa-shield-halved",
                    Order = 6
                }
            );
            await db.SaveChangesAsync();
        }
    }
}