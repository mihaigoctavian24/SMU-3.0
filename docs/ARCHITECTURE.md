# Arhitectura Tehnica SMU 3.0

## Cuprins

1. [Overview](#overview)
2. [Stack Tehnologic](#stack-tehnologic)
3. [Arhitectura Aplicatiei](#arhitectura-aplicatiei)
4. [Model Date](#model-date)
5. [Securitate](#securitate)
6. [Performanta](#performanta)
7. [Deployment](#deployment)

---

## Overview

SMU 3.0 foloseste o arhitectura **Blazor Server** cu un singur proiect, eliminand complexitatea separarii frontend/backend. Comunicarea cu baza de date se face direct prin Entity Framework Core catre PostgreSQL (Supabase).

### Principii Arhitecturale

- **KISS** - Keep It Simple, Stupid
- **YAGNI** - You Aren't Gonna Need It
- **DRY** - Don't Repeat Yourself
- **Single Responsibility** - Fiecare componenta are un scop clar

### Diagrama High-Level

```
┌─────────────────────────────────────────────────────────────────┐
│                         Browser                                  │
│                     (SignalR WebSocket)                         │
└─────────────────────────┬───────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│                    Azure App Service                             │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                  Blazor Server App                          │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │ │
│  │  │  Components  │  │   Services   │  │  ASP.NET Identity │  │ │
│  │  │   (Razor)    │  │   (Business) │  │  (Auth/Authz)     │  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────────┘  │ │
│  │                           │                                  │ │
│  │  ┌────────────────────────▼─────────────────────────────┐   │ │
│  │  │              Entity Framework Core                    │   │ │
│  │  │                  (DbContext)                          │   │ │
│  │  └────────────────────────┬─────────────────────────────┘   │ │
│  └───────────────────────────┼──────────────────────────────────┘ │
└──────────────────────────────┼──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                     Supabase PostgreSQL                          │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────┐  │
│  │   Users    │  │   Grades   │  │ Attendance │  │  Requests │  │
│  └────────────┘  └────────────┘  └────────────┘  └───────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Stack Tehnologic

### Frontend

| Tehnologie | Versiune | Scop |
|------------|----------|------|
| Blazor Server | .NET 8 | Framework UI |
| Radzen Blazor | 5.5.4 | UI Components (Material Theme) |
| Material Icons | Built-in | Iconografie (via Radzen) |
| SignalR | Built-in | Real-time updates |

### Backend

| Tehnologie | Versiune | Scop |
|------------|----------|------|
| ASP.NET Core | 8.0 | Web framework |
| Entity Framework Core | 8.0 | ORM |
| ASP.NET Identity | 8.0 | Autentificare |
| Npgsql | 8.0 | PostgreSQL driver |

### Infrastructure

| Serviciu | Provider | Scop |
|----------|----------|------|
| Database | Supabase | PostgreSQL hosting |
| Hosting | Azure App Service | Application hosting |
| CI/CD | GitHub Actions | Automated deployment |

---

## Arhitectura Aplicatiei

### Structura Foldere

```
src/SMU.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        # Layout principal (Radzen - includes sidebar & header)
│   │   └── ReconnectModal.razor    # Modal reconectare SignalR
│   ├── Shared/
│   │   ├── DataTable.razor         # Tabel generic reutilizabil
│   │   ├── Modal.razor             # Modal generic
│   │   ├── ConfirmDialog.razor     # Dialog confirmare
│   │   ├── LoadingSpinner.razor    # Indicator incarcare
│   │   └── NotificationBell.razor  # Bell notificari
│   └── Pages/
│       ├── Dashboard/
│       │   ├── StudentDashboard.razor
│       │   ├── ProfessorDashboard.razor
│       │   └── AdminDashboard.razor
│       ├── Students/
│       │   ├── StudentsList.razor
│       │   └── StudentDetails.razor
│       ├── Grades/
│       │   ├── GradesCatalog.razor
│       │   └── GradeApproval.razor
│       ├── Attendance/
│       │   ├── AttendanceView.razor
│       │   └── AttendanceReport.razor
│       ├── Requests/
│       │   ├── RequestsList.razor
│       │   └── RequestForm.razor
│       ├── Auth/
│       │   ├── Login.razor
│       │   └── AccessDenied.razor
│       └── Admin/
│           ├── UsersManagement.razor
│           └── SystemSettings.razor
├── Data/
│   ├── ApplicationDbContext.cs     # EF Core DbContext
│   ├── Entities/                   # Domain models
│   │   ├── ApplicationUser.cs
│   │   ├── Faculty.cs
│   │   ├── Program.cs
│   │   ├── Group.cs
│   │   ├── Course.cs
│   │   ├── Grade.cs
│   │   ├── Attendance.cs
│   │   ├── Request.cs
│   │   └── Notification.cs
│   └── Configurations/             # EF Fluent API configs
│       ├── UserConfiguration.cs
│       └── GradeConfiguration.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IStudentService.cs
│   │   ├── IGradeService.cs
│   │   └── INotificationService.cs
│   └── Implementations/
│       ├── StudentService.cs
│       ├── GradeService.cs
│       └── NotificationService.cs
├── Authorization/
│   ├── Policies.cs                 # Authorization policies
│   └── Requirements/               # Custom requirements
├── wwwroot/
│   ├── css/
│   │   └── app.css                 # Minimal custom styles
│   └── js/
│       └── app.js                  # Minimal JS if needed
├── appsettings.json
└── Program.cs
```

### Dependency Injection

```csharp
// Program.cs - Service Registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Business Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Authorization Policies
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageGrades", policy =>
        policy.RequireRole("Admin", "Dean", "Professor"));
    options.AddPolicy("CanApproveGrades", policy =>
        policy.RequireRole("Admin", "Dean"));
});
```

---

## Model Date

### Entity Relationship Diagram

```
┌──────────────────┐       ┌──────────────────┐
│     Faculty      │       │  ApplicationUser │
├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │
│ Name             │◄──────│ FacultyId (FK)   │
│ Code             │       │ FirstName        │
│ Description      │       │ LastName         │
└────────┬─────────┘       │ Email            │
         │                 │ Role (Enum)      │
         │                 │ IsActive         │
         │                 └────────┬─────────┘
         │                          │
┌────────▼─────────┐       ┌────────▼─────────┐
│     Program      │       │      Group       │
├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │
│ FacultyId (FK)   │       │ ProgramId (FK)   │
│ Name             │◄──────│ Name             │
│ DurationYears    │       │ Year             │
│ Degree           │       │ Section          │
└────────┬─────────┘       └────────┬─────────┘
         │                          │
┌────────▼─────────┐       ┌────────▼─────────┐
│     Course       │       │     Student      │
├──────────────────┤       │  (extends User)  │
│ Id (PK)          │       ├──────────────────┤
│ ProgramId (FK)   │       │ GroupId (FK)     │
│ ProfessorId (FK) │       │ StudentNumber    │
│ Name             │       │ EnrollmentDate   │
│ Credits          │       │ Status           │
│ Semester         │       └────────┬─────────┘
│ Year             │                │
└────────┬─────────┘                │
         │                          │
         │    ┌─────────────────────┴────────────────────┐
         │    │                                          │
┌────────▼────▼────┐       ┌─────────────────┐  ┌───────▼────────┐
│      Grade       │       │   Attendance    │  │    Request     │
├──────────────────┤       ├─────────────────┤  ├────────────────┤
│ Id (PK)          │       │ Id (PK)         │  │ Id (PK)        │
│ StudentId (FK)   │       │ StudentId (FK)  │  │ StudentId (FK) │
│ CourseId (FK)    │       │ CourseId (FK)   │  │ Type           │
│ Value            │       │ Date            │  │ Status         │
│ Status (Enum)    │       │ Status (Enum)   │  │ Reason         │
│ GradedBy (FK)    │       │ MarkedBy (FK)   │  │ ProcessedBy    │
│ ApprovedBy (FK)  │       └─────────────────┘  │ ApprovedBy     │
│ GradedAt         │                            └────────────────┘
│ ApprovedAt       │
└──────────────────┘
```

### Enum Definitions

```csharp
public enum UserRole
{
    Student,
    Professor,
    Secretary,
    Dean,
    Rector,
    Admin
}

public enum GradeStatus
{
    Draft,      // Profesor a introdus, nu e finala
    Pending,    // Trimisa pentru aprobare
    Approved,   // Aprobata de Decan
    Rejected    // Respinsa, necesita modificari
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Excused,
    Late
}

public enum RequestType
{
    Certificate,        // Adeverinta student
    GradeReview,       // Contestatie nota
    Leave,             // Cerere invoire
    Transfer,          // Transfer grupa/program
    Other
}

public enum RequestStatus
{
    Submitted,
    InProgress,
    Approved,
    Rejected,
    Completed
}
```

---

## Securitate

### Autentificare

ASP.NET Identity cu cookies pentru Blazor Server:

```csharp
// Cookie configuration
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
});
```

### Autorizare

**Role-Based Access Control (RBAC):**

```csharp
// Component-level authorization
@attribute [Authorize(Roles = "Admin,Dean")]
@page "/admin/users"

// Programmatic checks
@inject AuthenticationStateProvider AuthState

@code {
    private async Task<bool> CanApproveGrades()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        return authState.User.IsInRole("Admin") ||
               authState.User.IsInRole("Dean");
    }
}
```

**Resource-Based Authorization:**

```csharp
// Profesorul poate edita doar notele la cursurile lui
public class GradeAuthorizationHandler :
    AuthorizationHandler<SameOwnerRequirement, Grade>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOwnerRequirement requirement,
        Grade grade)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (grade.GradedById.ToString() == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### Audit Logging

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; }          // CREATE, UPDATE, DELETE
    public string EntityType { get; set; }       // Grade, Request, etc.
    public Guid EntityId { get; set; }
    public string OldValues { get; set; }        // JSON
    public string NewValues { get; set; }        // JSON
    public string IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## Performanta

### Database Optimization

```csharp
// Indexes definite in Configurations
modelBuilder.Entity<Grade>()
    .HasIndex(g => new { g.StudentId, g.CourseId })
    .IsUnique();

modelBuilder.Entity<Attendance>()
    .HasIndex(a => new { a.StudentId, a.CourseId, a.Date })
    .IsUnique();

modelBuilder.Entity<ApplicationUser>()
    .HasIndex(u => u.Email)
    .IsUnique();
```

### Caching Strategy

```csharp
// In-memory cache pentru date statice
builder.Services.AddMemoryCache();

// Usage in service
public class FacultyService : IFacultyService
{
    private readonly IMemoryCache _cache;

    public async Task<List<Faculty>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync("faculties", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _context.Faculties.ToListAsync();
        });
    }
}
```

### SignalR Optimization

```csharp
// Grupuri pentru notificari targetate
public class NotificationHub : Hub
{
    public async Task JoinFacultyGroup(string facultyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"faculty-{facultyId}");
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}

// Sending targeted notifications
await _hubContext.Clients.Group($"user-{userId}")
    .SendAsync("ReceiveNotification", notification);
```

---

## Deployment

### Azure App Service Configuration

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '18'

    - name: Publish
      run: dotnet publish -c Release -o ./publish

    - name: Deploy to Azure
      uses: azure/webapps-deploy@v3
      with:
        app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: ./publish
```

### Environment Variables

```bash
# Production settings via Azure App Configuration
ConnectionStrings__DefaultConnection=Host=xxx.supabase.co;Database=postgres;Username=xxx;Password=xxx
ASPNETCORE_ENVIRONMENT=Production
```

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddCheck<SignalRHealthCheck>("signalr");

app.MapHealthChecks("/health");
```

---

## Diagrame Aditionale

### Flow Autentificare

```
┌─────────┐     ┌─────────────┐     ┌──────────────┐     ┌────────┐
│ Browser │────►│ Login Page  │────►│ ASP.NET      │────►│ Cookie │
│         │     │             │     │ Identity     │     │ Set    │
└─────────┘     └─────────────┘     └──────────────┘     └────────┘
                                           │
                                           ▼
                                    ┌──────────────┐
                                    │ PostgreSQL   │
                                    │ (Users Table)│
                                    └──────────────┘
```

### Flow Aprobare Nota

```
┌───────────┐    ┌────────────┐    ┌───────────┐    ┌──────────┐
│ Profesor  │───►│ Add Grade  │───►│ Status:   │───►│ Notifica │
│           │    │ (Draft)    │    │ Pending   │    │ Decan    │
└───────────┘    └────────────┘    └───────────┘    └────┬─────┘
                                                         │
                                                         ▼
┌───────────┐    ┌────────────┐    ┌───────────┐    ┌──────────┐
│ Student   │◄───│ Notificare │◄───│ Status:   │◄───│ Decan    │
│ vede nota │    │            │    │ Approved  │    │ Aproba   │
└───────────┘    └────────────┘    └───────────┘    └──────────┘
```

---

## Referinte

- [Blazor Server Documentation](https://docs.microsoft.com/aspnet/core/blazor/hosting-models#blazor-server)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [ASP.NET Identity](https://docs.microsoft.com/aspnet/core/security/authentication/identity)
- [Radzen Blazor Components](https://blazor.radzen.com/docs/)
- [Supabase PostgreSQL](https://supabase.com/docs/guides/database)
