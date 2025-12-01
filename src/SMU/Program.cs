using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SMU.Components;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services;
using SMU.Services.Jobs;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Database Configuration (Supabase PostgreSQL)
// ===========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Build NpgsqlDataSource with enum mappings at ADO.NET level
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<ProgramType>("program_type");
dataSourceBuilder.MapEnum<StudentStatus>("student_status");
dataSourceBuilder.MapEnum<GradeType>("grade_type");
dataSourceBuilder.MapEnum<GradeStatus>("grade_status");
dataSourceBuilder.MapEnum<AttendanceStatus>("attendance_status");
dataSourceBuilder.MapEnum<NotificationType>("notification_type");
dataSourceBuilder.MapEnum<RequestStatus>("request_status");
dataSourceBuilder.MapEnum<RequestType>("request_type");
dataSourceBuilder.MapEnum<RiskLevel>("risk_level");
dataSourceBuilder.MapEnum<ExportType>("export_type");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        // Connection resilience
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        // Map PostgreSQL enums to C# enums (EF Core level)
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
        npgsqlOptions.MapEnum<ExportType>("export_type");
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
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

// ===========================================
// Authorization Policies
// ===========================================
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("Role", "Admin"))
    .AddPolicy("CanManageGrades", policy =>
        policy.RequireClaim("Role", "Professor", "Secretary", "Dean", "Admin"))
    .AddPolicy("CanApproveGrades", policy =>
        policy.RequireClaim("Role", "Dean", "Admin"))
    .AddPolicy("CanManageStudents", policy =>
        policy.RequireClaim("Role", "Secretary", "Dean", "Admin"))
    .AddPolicy("CanManageUsers", policy =>
        policy.RequireClaim("Role", "Admin"))
    .AddPolicy("CanViewReports", policy =>
        policy.RequireClaim("Role", "Dean", "Rector", "Admin"))
    .AddPolicy("FacultyStaff", policy =>
        policy.RequireClaim("Role", "Professor", "Secretary", "Dean"))
    .AddPolicy("CanCreateRequests", policy =>
        policy.RequireClaim("Role", "Student"))
    .AddPolicy("CanProcessRequests", policy =>
        policy.RequireClaim("Role", "Secretary", "Dean", "Admin"))
    .AddPolicy("CanManageFaculties", policy =>
        policy.RequireClaim("Role", "Admin", "Dean"))
    .AddPolicy("CanManagePrograms", policy =>
        policy.RequireClaim("Role", "Admin", "Secretary"))
    .AddPolicy("CanManageGroups", policy =>
        policy.RequireClaim("Role", "Admin", "Secretary"));

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
// Localization Configuration
// ===========================================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("ro-RO"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("ro-RO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Use cookie provider first, then accept-language header
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// ===========================================
// Application Services
// ===========================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDocumentRequestService, DocumentRequestService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IExportHistoryService, ExportHistoryService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();

// Faza 8: Predictive Analytics Services
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// UI Services
builder.Services.AddScoped<IKeyboardShortcutService, KeyboardShortcutService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

// Background Jobs
builder.Services.AddScoped<DailySnapshotJob>();
builder.Services.AddScoped<RiskCalculationJob>();
builder.Services.AddHostedService<JobSchedulerService>();

// HttpClient for internal API calls (used by Blazor components)
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
});

// ===========================================
// SignalR Configuration
// ===========================================
builder.Services.AddSignalR();

// ===========================================
// Blazor Components
// ===========================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// Add controllers for CultureController
builder.Services.AddControllers();

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

// Enable request localization (must be before routing)
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();

// Status code pages must be early in the pipeline
app.UseStatusCodePagesWithReExecute("/not-found", "?statusCode={0}");

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// ===========================================
// Authentication Endpoints (Minimal API)
// ===========================================
app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    LoginRequest request) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null || !user.IsActive)
    {
        return Results.BadRequest(new { error = "Email sau parolă incorectă." });
    }

    var result = await signInManager.PasswordSignInAsync(
        user.UserName!,
        request.Password,
        request.RememberMe,
        lockoutOnFailure: true);

    if (result.Succeeded)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var redirectUrl = user.Role switch
        {
            UserRole.Student => "/dashboard",
            UserRole.Professor => "/dashboard",
            UserRole.Secretary => "/studenti",
            UserRole.Dean => "/dashboard",
            UserRole.Rector => "/dashboard",
            UserRole.Admin => "/dashboard",
            _ => "/dashboard"
        };

        return Results.Ok(new { redirectUrl });
    }

    if (result.IsLockedOut)
    {
        return Results.BadRequest(new { error = "Contul este blocat temporar. Încercați mai târziu." });
    }

    return Results.BadRequest(new { error = "Email sau parolă incorectă." });
}).AllowAnonymous();

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/logout", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    httpContext.Response.Redirect("/login");
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map controllers (for CultureController)
app.MapControllers();

// ===========================================
// SignalR Hubs
// ===========================================
app.MapHub<SMU.Hubs.NotificationHub>("/notificationHub");

// ===========================================
// Development: Reset demo user passwords (non-blocking)
// ===========================================
if (app.Environment.IsDevelopment())
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(5000); // Wait for app to fully start
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var demoPassword = "Demo123!";
            var demoUsers = new[] { "admin@smu.edu", "rector@smu.edu", "decan@smu.edu", "secretar@smu.edu", "profesor@smu.edu", "student@smu.edu" };

            foreach (var email in demoUsers)
            {
                try
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(user);
                        var result = await userManager.ResetPasswordAsync(user, token, demoPassword);
                        if (result.Succeeded)
                        {
                            logger.LogInformation("Reset password for demo user: {Email}", email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Failed to reset password for {Email}: {Error}", email, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Password reset failed: {ex.Message}");
        }
    });
}

app.Run();

// ===========================================
// Request/Response DTOs
// ===========================================
public record LoginRequest(string Email, string Password, bool RememberMe = false);
