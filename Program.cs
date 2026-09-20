using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using NavGuru.Configuration;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.Services;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");
if (port is not null)
{
    builder.WebHost.UseUrls($"http://+:{port}");
}
// ---- Database ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=NavGuruDb.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsProduction())
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// ---- Identity ----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ---- Cookie auth redirect ----
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ═══════════════════════════════════════════════════════
// MICROSOFT ENTRA ID — only register if ClientId is set
// ═══════════════════════════════════════════════════════
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    builder.Services.AddAuthentication()
        .AddMicrosoftIdentityWebApp(options =>
        {
            builder.Configuration.GetSection("AzureAd").Bind(options);

            options.SignInScheme = "Identity.External";
            options.ResponseType = "code";

            options.Events = new OpenIdConnectEvents
            {
                OnRemoteFailure = context =>
                {
                    Console.WriteLine("=== OIDC REMOTE FAILURE ===");
                    Console.WriteLine($"Error: {context.Failure?.Message}");
                    context.HandleResponse();
                    context.Response.Redirect("/Account/Login?error=oidc_failed");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine("=== TOKEN VALIDATED ===");
                    Console.WriteLine($"SignInScheme in token validated: {context.Options.SignInScheme}");
                    return Task.CompletedTask;
                }
            };
        },
        openIdConnectScheme: "MicrosoftEntra",
        cookieScheme: null);
}
else
{
    Console.WriteLine("[NavGuru] Entra ID SSO disabled — AzureAd:ClientId not configured.");
}  // ← THE CRITICAL LINE — tells M.I.W. not to use its own cookie

// ---- Typed configuration ----
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<MapsOptions>(builder.Configuration.GetSection(MapsOptions.SectionName));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection(FeatureFlags.SectionName));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<QrOptions>(
    builder.Configuration.GetSection(QrOptions.SectionName));
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.Configure<GrokOptions>(
    builder.Configuration.GetSection(GrokOptions.SectionName));

// ---- Caching ----
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient("Grok");
// ---- MVC + App services ----
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<FaqService>();
builder.Services.AddScoped<IStudentNumberGenerator, StudentNumberGenerator>();
builder.Services.AddScoped<IFaqAssistantService, FaqAssistantService>();

var app = builder.Build();

// ---- Seeder ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsProduction())
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();     // ← must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();