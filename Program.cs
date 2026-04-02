using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Services;
using Hangfire;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// DATABASE
// ----------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// ----------------------
// DATA PROTECTION (Persistent Keys for Render)
// ----------------------
var dataProtectionPath = Path.Combine(Path.GetTempPath(), "CodeSolvedTracker-Keys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("CodeSolvedTracker");

// ----------------------
// SESSION
// ----------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ----------------------
// MVC & HTTP CLIENT
// ----------------------
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// ----------------------
// HANGFIRE (SQL Server or in-memory fallback)
// ----------------------
var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection");
if (!string.IsNullOrEmpty(hangfireConnection))
{
    builder.Services.AddHangfire(config => config.UseSqlServerStorage(hangfireConnection));
}
else
{
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
}
builder.Services.AddHangfireServer();

// ----------------------
// JWT CONFIGURATION
// ----------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CodeSolvedTracker_SuperSecureKey_2026_!@#$%^&*()";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodeSolvedTracker";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CodeSolvedTrackerUsers";
var key = Encoding.UTF8.GetBytes(jwtKey);

// ----------------------
// AUTHENTICATION
// ----------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
})
.AddGitHub(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "";
    options.ClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? "";
    options.CallbackPath = "/signin-github";
    options.SaveTokens = true;
    options.Scope.Add("user:email");
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ----------------------
// AUTHORIZATION & SERVICES
// ----------------------
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SyncService>();

var app = builder.Build();

// ----------------------
// DATABASE INITIALIZATION
// ----------------------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    // Optional: Seed admin/test users
}

// ----------------------
// FORWARDED HEADERS (Render Proxy Fix)
// ----------------------
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ----------------------
// MIDDLEWARE PIPELINE
// ----------------------
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

// ----------------------
// ROUTING
// ----------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ----------------------
// RUN ON RENDER PORT
// ----------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");