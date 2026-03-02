using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;
using RideGhana.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Priority:
//   1. DATABASE_URL env var (Railway injects postgresql:// URI automatically)
//   2. ConnectionStrings:DefaultConnection starting with "Host=" → PostgreSQL
//   3. Anything else (Data Source=...) → SQLite (local dev)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

if (!string.IsNullOrEmpty(databaseUrl))
{
    connectionString = ConvertRailwayDatabaseUrl(databaseUrl);
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
}
else if (connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString));
}

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// ── App services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient<PaystackService>((sp, client) =>
{
    var key = sp.GetRequiredService<IConfiguration>()["Paystack:SecretKey"] ?? string.Empty;
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Reservations/Index");
    options.Conventions.AuthorizePage("/Checkout/Index");
    options.Conventions.AuthorizeFolder("/Admin", "Admin");
})
.AddRazorRuntimeCompilation();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ── Stripe ────────────────────────────────────────────────────────────────────
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static string ConvertRailwayDatabaseUrl(string url)
{
    // Railway format: postgresql://user:password@host:port/database
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
