using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using NavGuru.Configuration;
using NavGuru.Data;
using NavGuru.Models;
using NavGuru.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Render port binding ----
var port = Environment.GetEnvironmentVariable("PORT");
if (port is not null)
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

// ---- Trust Render's reverse proxy ----
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

// ---- Cookie auth ----
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ---- Entra ID (only if configured) ----
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
                OnRedirectToIdentityProvider = context =>
                {
                    // Force HTTPS in the redirect URI — Render terminates SSL at the proxy
                    var uri = context.ProtocolMessage.RedirectUri;
                    if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        context.ProtocolMessage.RedirectUri = "https://" + uri.Substring(7);
                    }
                    return Task.CompletedTask;
                },
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
}

// ---- Typed configuration ----
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<MapsOptions>(builder.Configuration.GetSection(MapsOptions.SectionName));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection(FeatureFlags.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<QrOptions>(builder.Configuration.GetSection(QrOptions.SectionName));
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.Configure<GrokOptions>(builder.Configuration.GetSection(GrokOptions.SectionName));

// ---- Caching ----
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient("Grok");

// ---- MVC + Services ----
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IStudentNumberGenerator, StudentNumberGenerator>();
builder.Services.AddScoped<IFaqAssistantService, FaqAssistantService>();
builder.Services.AddScoped<FaqService>();

var app = builder.Build();

// ---- FIRST middleware ----
app.UseForwardedHeaders();

// ---- Error handling + HTTPS ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ---- Seeder (after middleware, before Run) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsProduction())
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();