# API Documentation

SMU 3.0 foloseste Blazor Server, deci nu expune un REST API traditional. Comunicarea se face prin SignalR pentru real-time updates si servicii injectate pentru operatii de date.

## Cuprins

1. [Services Architecture](#services-architecture)
2. [Service Interfaces](#service-interfaces)
3. [SignalR Hubs](#signalr-hubs)
4. [Data Transfer Objects](#data-transfer-objects)

---

## Services Architecture

### Dependency Injection Pattern

```csharp
// Program.cs - Service Registration
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
```

### Service Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                     Blazor Components                        │
│                   (Inject IXxxService)                       │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                      Services Layer                          │
│   - Business Logic                                           │
│   - Validation                                               │
│   - Authorization checks                                     │
│   - Transaction management                                   │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                   Entity Framework Core                      │
│                    (ApplicationDbContext)                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Service Interfaces

### IStudentService

```csharp
public interface IStudentService
{
    // Query Operations
    Task<PagedResult<StudentDto>> GetStudentsAsync(StudentFilter filter, PagingOptions paging);
    Task<StudentDto?> GetByIdAsync(Guid id);
    Task<StudentDto?> GetByStudentNumberAsync(string studentNumber);
    Task<List<StudentDto>> GetByGroupAsync(Guid groupId);
    Task<List<StudentDto>> GetByProgramAsync(Guid programId);

    // Command Operations
    Task<StudentDto> CreateAsync(CreateStudentDto dto);
    Task<StudentDto> UpdateAsync(Guid id, UpdateStudentDto dto);
    Task DeleteAsync(Guid id);
    Task<bool> TransferToGroupAsync(Guid studentId, Guid newGroupId);

    // Statistics
    Task<StudentStatistics> GetStatisticsAsync(Guid? facultyId = null);
}

// Filter class
public class StudentFilter
{
    public string? SearchTerm { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? GroupId { get; set; }
    public int? Year { get; set; }
    public StudentStatus? Status { get; set; }
}
```

### IGradeService

```csharp
public interface IGradeService
{
    // Query Operations
    Task<List<GradeDto>> GetStudentGradesAsync(Guid studentId, int? year = null, int? semester = null);
    Task<List<GradeDto>> GetCourseGradesAsync(Guid courseId);
    Task<List<GradeDto>> GetPendingApprovalsAsync(Guid facultyId);
    Task<GradeDto?> GetByIdAsync(Guid id);

    // Command Operations (Professor)
    Task<GradeDto> AddGradeAsync(CreateGradeDto dto, Guid professorId);
    Task<GradeDto> UpdateGradeAsync(Guid id, UpdateGradeDto dto, Guid professorId);
    Task DeleteGradeAsync(Guid id, Guid professorId);
    Task SubmitForApprovalAsync(Guid gradeId, Guid professorId);

    // Command Operations (Dean)
    Task ApproveGradeAsync(Guid gradeId, Guid deanId);
    Task RejectGradeAsync(Guid gradeId, Guid deanId, string reason);
    Task BulkApproveAsync(List<Guid> gradeIds, Guid deanId);

    // Statistics
    Task<GradeStatistics> GetStatisticsAsync(Guid courseId);
    Task<decimal> GetStudentAverageAsync(Guid studentId, int? year = null);
}
```

### IAttendanceService

```csharp
public interface IAttendanceService
{
    // Query Operations
    Task<List<AttendanceDto>> GetStudentAttendanceAsync(Guid studentId, Guid? courseId = null);
    Task<List<AttendanceDto>> GetCourseAttendanceAsync(Guid courseId, DateTime? date = null);
    Task<AttendanceReport> GetAttendanceReportAsync(Guid studentId, int year, int semester);

    // Command Operations (Professor)
    Task<AttendanceDto> MarkAttendanceAsync(CreateAttendanceDto dto, Guid professorId);
    Task BulkMarkAttendanceAsync(List<CreateAttendanceDto> dtos, Guid professorId);
    Task UpdateAttendanceAsync(Guid id, UpdateAttendanceDto dto, Guid professorId);

    // Command Operations (Student)
    Task RequestExcuseAsync(Guid attendanceId, Guid studentId, string reason, string? documentUrl);

    // Statistics
    Task<AttendanceStatistics> GetStatisticsAsync(Guid courseId);
    Task<decimal> GetStudentAttendanceRateAsync(Guid studentId, Guid courseId);
}
```

### IRequestService

```csharp
public interface IRequestService
{
    // Query Operations
    Task<PagedResult<RequestDto>> GetRequestsAsync(RequestFilter filter, PagingOptions paging);
    Task<List<RequestDto>> GetStudentRequestsAsync(Guid studentId);
    Task<List<RequestDto>> GetPendingRequestsAsync(Guid? facultyId = null);
    Task<RequestDto?> GetByIdAsync(Guid id);

    // Command Operations (Student)
    Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, Guid studentId);
    Task CancelRequestAsync(Guid id, Guid studentId);

    // Command Operations (Secretary)
    Task<RequestDto> ProcessRequestAsync(Guid id, Guid secretaryId);
    Task<RequestDto> CompleteRequestAsync(Guid id, Guid secretaryId, string? documentUrl);

    // Command Operations (Dean)
    Task<RequestDto> ApproveRequestAsync(Guid id, Guid deanId);
    Task<RequestDto> RejectRequestAsync(Guid id, Guid deanId, string reason);

    // Statistics
    Task<RequestStatistics> GetStatisticsAsync(Guid? facultyId = null);
}
```

### INotificationService

```csharp
public interface INotificationService
{
    // Query Operations
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(Guid userId);

    // Command Operations
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
    Task DeleteNotificationAsync(Guid notificationId, Guid userId);

    // Broadcast Operations
    Task NotifyUserAsync(Guid userId, string title, string message, NotificationType type);
    Task NotifyGroupAsync(Guid groupId, string title, string message, NotificationType type);
    Task NotifyFacultyAsync(Guid facultyId, string title, string message, NotificationType type);
    Task NotifyRoleAsync(UserRole role, string title, string message, NotificationType type);
}
```

---

## SignalR Hubs

### NotificationHub

```csharp
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            // Add to role group
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (role != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinFacultyGroup(string facultyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"faculty-{facultyId}");
    }

    public async Task JoinGroupGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");
    }

    public async Task MarkNotificationRead(Guid notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await _notificationService.MarkAsReadAsync(notificationId, Guid.Parse(userId));
        }
    }
}
```

### Client-Side Integration (Blazor)

```csharp
// In component
@inject NavigationManager Navigation
@implements IAsyncDisposable

private HubConnection? _hubConnection;
private List<NotificationDto> _notifications = new();

protected override async Task OnInitializedAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/hubs/notifications"))
        .WithAutomaticReconnect()
        .Build();

    _hubConnection.On<NotificationDto>("ReceiveNotification", (notification) =>
    {
        _notifications.Insert(0, notification);
        InvokeAsync(StateHasChanged);
    });

    await _hubConnection.StartAsync();
}

public async ValueTask DisposeAsync()
{
    if (_hubConnection is not null)
    {
        await _hubConnection.DisposeAsync();
    }
}
```

---

## Data Transfer Objects

### Student DTOs

```csharp
public class StudentDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public int Year { get; set; }
    public StudentStatus Status { get; set; }
    public DateTime EnrollmentDate { get; set; }
}

public class CreateStudentDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Guid GroupId { get; set; }
}

public class UpdateStudentDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public Guid? GroupId { get; set; }
    public StudentStatus? Status { get; set; }
}
```

### Grade DTOs

```csharp
public class GradeDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public GradeStatus Status { get; set; }
    public string GradedByName { get; set; } = string.Empty;
    public DateTime GradedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreateGradeDto
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [Range(1, 10)]
    public decimal Value { get; set; }

    public string? Notes { get; set; }
}
```

### Paging

```csharp
public class PagingOptions
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

---

## Error Handling

### Service Exceptions

```csharp
public class ServiceException : Exception
{
    public int StatusCode { get; }

    public ServiceException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : ServiceException
{
    public NotFoundException(string entity, Guid id)
        : base($"{entity} with ID {id} not found", 404)
    {
    }
}

public class UnauthorizedException : ServiceException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401)
    {
    }
}

public class ValidationException : ServiceException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Validation failed", 400)
    {
        Errors = errors;
    }
}
```

### Global Error Handler (Blazor)

```csharp
// ErrorBoundary.razor
@inherits ErrorBoundary

@if (CurrentException is not null)
{
    <div class="bg-red-50 border border-red-200 rounded-lg p-4">
        <h3 class="text-red-800 font-medium">A aparut o eroare</h3>
        <p class="text-red-600 text-sm">@GetUserFriendlyMessage(CurrentException)</p>
        <button @onclick="Recover" class="mt-2 text-red-700 underline">
            Incearca din nou
        </button>
    </div>
}
else
{
    @ChildContent
}

@code {
    private string GetUserFriendlyMessage(Exception ex) => ex switch
    {
        NotFoundException => "Resursa solicitata nu a fost gasita.",
        UnauthorizedException => "Nu aveti permisiunea pentru aceasta actiune.",
        ValidationException ve => string.Join(", ", ve.Errors.SelectMany(e => e.Value)),
        _ => "A aparut o eroare neasteptata. Va rugam incercati din nou."
    };
}
```
