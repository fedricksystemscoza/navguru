using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NavGuru.Models;
using NavGuru.ViewModels;

namespace NavGuru.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signIn,
                             UserManager<ApplicationUser> userManager)
    {
        _signIn = signIn;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var info = await _signIn.GetExternalLoginInfoAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user is null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(vm);
        }

        var result = await _signIn.PasswordSignInAsync(user, vm.Password, false, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(vm);
        }

        // RBAC redirect (matches your diagram's Admin vs Student split)
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return RedirectToAction("Index", "Admin");

        return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
    }

    // ============================================================
    // PROFILE — GET + POST
    // ============================================================

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        var roles = await _userManager.GetRolesAsync(user);

        var vm = new ProfileViewModel
        {
            // Read-only identity fields
            FullName = user.FullName,
            StudentNumber = user.StudentNumber,
            Email = user.Email,
            Faculty = user.Faculty,
            RoleName = roles.FirstOrDefault() ?? "Student",
            CreatedAt = user.CreatedAt,

            // Editable preferences
            PhoneNumber = user.PhoneNumber,
            Language = user.Language,
            EmailNotifications = user.EmailNotifications,
            PushNotifications = user.PushNotifications,
            EventReminders = user.EventReminders,
            DarkMode = user.DarkMode
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            // Re-populate read-only fields — they can't come from the form
            vm.FullName = user.FullName;
            vm.StudentNumber = user.StudentNumber;
            vm.Email = user.Email;
            vm.Faculty = user.Faculty;
            vm.CreatedAt = user.CreatedAt;
            vm.RoleName = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Student";
            return View(vm);
        }

        user.PhoneNumber = vm.PhoneNumber;
        user.Language = vm.Language;
        user.EmailNotifications = vm.EmailNotifications;
        user.PushNotifications = vm.PushNotifications;
        user.EventReminders = vm.EventReminders;
        user.DarkMode = vm.DarkMode;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            vm.FullName = user.FullName;
            vm.StudentNumber = user.StudentNumber;
            vm.Email = user.Email;
            vm.Faculty = user.Faculty;
            vm.CreatedAt = user.CreatedAt;
            vm.RoleName = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Student";
            return View(vm);
        }
        Response.Cookies.Append("navguru-theme", vm.DarkMode ? "dark" : "light", new CookieOptions
        {
            HttpOnly = false,                 // JS needs to read it for localStorage sync
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            SameSite = SameSiteMode.Lax
        });

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        return View(new SettingsViewModel
        {
            DarkMode = user.DarkMode,
            EmailNotifications = user.EmailNotifications,
            PushNotifications = user.PushNotifications,
            EventReminders = user.EventReminders,
            OfflineCacheEnabled = user.OfflineCacheEnabled,
            Language = user.Language
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SettingsViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        user.DarkMode = vm.DarkMode;
        user.EmailNotifications = vm.EmailNotifications;
        user.PushNotifications = vm.PushNotifications;
        user.EventReminders = vm.EventReminders;
        user.OfflineCacheEnabled = vm.OfflineCacheEnabled;
        user.Language = vm.Language;

        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCache()
    {
        // Placeholder — extend with actual cache clearing
        TempData["Success"] = "Offline cache cleared.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        await _signIn.SignOutAsync();
        await _userManager.DeleteAsync(user);

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        await _signIn.RefreshSignInAsync(user);

        TempData["Success"] = "Password changed.";
        return RedirectToAction(nameof(Profile));
    }


    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        Console.WriteLine($"=== ExternalLogin POST. provider={provider} ===");

        var redirect = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var props = _signIn.ConfigureExternalAuthenticationProperties(provider, redirect);
        return Challenge(props, provider);
    }
    //[HttpPost, ValidateAntiForgeryToken]
    //public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    //{
    //    Console.WriteLine($"=== ExternalLogin POST. provider={provider} ===");

    //    var redirect = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
    //    var props = _signIn.ConfigureExternalAuthenticationProperties(provider, redirect);

    //    Console.WriteLine($"=== Properties.SignInScheme = {props?.SignInScheme} ===");
    //    Console.WriteLine($"=== Properties.RedirectUri = {props?.RedirectUri} ===");

    //    return Challenge(props, provider);
    //}


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {

        Console.WriteLine("=== CALLBACK HIT ===");   // ← add if missing
        var info = await _signIn.GetExternalLoginInfoAsync();
        Console.WriteLine($"info null? {info is null}");

        if (info is not null)
        {
            foreach (var c in info.Principal.Claims)
                Console.WriteLine($"  CLAIM {c.Type} = {c.Value}");
        }

        if (info is null)
        {
            Console.WriteLine("=== info WAS NULL — bailing to Login ===");
            TempData["Error"] = "External login failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }
        // Try existing external login
        var result = await _signIn.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser is not null && await _userManager.IsInRoleAsync(existingUser, "Admin"))
                return RedirectToAction("Index", "Admin");

            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
        }

        // First-time user — auto-provision
        var email =
       info.Principal.FindFirst("preferred_username")?.Value
       ?? info.Principal.FindFirst("email")?.Value
       ?? info.Principal.FindFirst("upn")?.Value
       ?? info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
       ?? info.Principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
       ?? info.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/emailaddress")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Could not read your email from Microsoft. Contact support.";
            return RedirectToAction(nameof(Login));
        }

        // Check if a local account with this email exists already
        var existingByEmail = await _userManager.FindByEmailAsync(email);
        if (existingByEmail is not null)
        {
            // Link the external login to the existing account
            await _userManager.AddLoginAsync(existingByEmail, info);
            await _signIn.SignInAsync(existingByEmail, isPersistent: false);

            if (await _userManager.IsInRoleAsync(existingByEmail, "Admin"))
                return RedirectToAction("Index", "Admin");

            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
        }

        // Create a fresh student account
        var fullName = info.Principal.FindFirst("name")?.Value ?? email;
        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(newUser, info);
            await _userManager.AddToRoleAsync(newUser, "Student");
            await _signIn.SignInAsync(newUser, isPersistent: false);

            TempData["Success"] = $"Welcome to NavBuddy, {fullName.Split(' ').First()}!";
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
        }

        // ===== DEBUG: Print exact failure reason =====
        foreach (var e in createResult.Errors)
            Console.WriteLine($"CREATE ERROR [{e.Code}]: {e.Description}");
        // =============================================

        TempData["Error"] = "Could not create your account: " +
            string.Join("; ", createResult.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Login));
    }

    private IActionResult? RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : null;
}
