using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMU.Components;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Database Configuration (Supabase PostgreSQL)
// ===========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configure PostgreSQL ENUMs
        npgsqlOptions.MapEnum<UserRole>("user_role");
        npgsqlOptions.MapEnum<ProgramType>("program_type");
        npgsqlOptions.MapEnum<StudentStatus>("student_status");
        npgsqlOptions.MapEnum<GradeType>("grade_type");
        npgsqlOptions.MapEnum<GradeStatus>("grade_status");
        npgsqlOptions.MapEnum<AttendanceStatus>("attendance_status");
        npgsqlOptions.MapEnum<NotificationType>("notification_type");
        npgsqlOptions.MapEnum<RequestStatus>("request_status");
        npgsqlOptions.MapEnum<RequestType>("request_type");
        npgsqlOptions.MapEnum<RiskLevel>("risk_level");

        // Connection resilience
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });

    // Development logging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===========================================
// ASP.NET Identity Configuration
// ===========================================
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings from configuration
    var passwordConfig = builder.Configuration.GetSection("PasswordPolicy");
    options.Password.RequireDigit = passwordConfig.GetValue("RequireDigit", true);
    options.Password.RequireLowercase = passwordConfig.GetValue("RequireLowercase", true);
    options.Password.RequireUppercase = passwordConfig.GetValue("RequireUppercase", true);
    options.Password.RequireNonAlphanumeric = passwordConfig.GetValue("RequireNonAlphanumeric", false);
    options.Password.RequiredLength = passwordConfig.GetValue("RequiredLength", 8);

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    var authConfig = builder.Configuration.GetSection("Authentication");
    options.SignIn.RequireConfirmedEmail = authConfig.GetValue("RequireConfirmedAccount", false);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===========================================
// Cookie Authentication Configuration
// ===========================================
var cookieExpireHours = builder.Configuration.GetValue("Authentication:CookieExpireHours", 8);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(cookieExpireHours);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

// ===========================================
// Application Services
// ===========================================
builder.Services.AddScoped<AuthService>();

// ===========================================
// Blazor Components
// ===========================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// ===========================================
// HTTP Pipeline Configuration
// ===========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStatusCodePagesWithReExecute("/not-found", "?statusCode={0}");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
