using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for student management
/// </summary>
public interface IStudentService
{
    Task<PagedResult<StudentListDto>> GetAllAsync(StudentFilter filter, int page = 1, int pageSize = 25);
    Task<StudentDetailDto?> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateStudentDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateStudentDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult> TransferGroupAsync(Guid studentId, Guid newGroupId);
    Task<List<StudentListDto>> ExportAsync(StudentFilter filter);
    Task<List<StudentListDto>> GetRecentAsync(int count = 10);
}

/// <summary>
/// Student management service implementation
/// </summary>
public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<StudentService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PagedResult<StudentListDto>> GetAllAsync(StudentFilter filter, int page = 1, int pageSize = 25)
    {
        var query = _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(s =>
                s.User.FirstName.ToLower().Contains(searchLower) ||
                s.User.LastName.ToLower().Contains(searchLower) ||
                s.User.Email.ToLower().Contains(searchLower) ||
                s.StudentNumber.ToLower().Contains(searchLower));
        }

        if (filter.GroupId.HasValue)
        {
            query = query.Where(s => s.GroupId == filter.GroupId);
        }

        if (filter.ProgramId.HasValue)
        {
            query = query.Where(s => s.Group!.ProgramId == filter.ProgramId);
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(s => s.Group!.Program.FacultyId == filter.FacultyId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(s => s.Status == filter.Status);
        }

        var totalCount = await query.CountAsync();

        var students = await query
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email ?? "",
                StudentNumber = s.StudentNumber,
                GroupName = s.Group != null ? s.Group.Name : "Neasignat",
                ProgramName = s.Group != null ? s.Group.Program.Name : "N/A",
                Status = s.Status
            })
            .ToListAsync();

        return new PagedResult<StudentListDto>
        {
            Items = students,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StudentDetailDto?> GetByIdAsync(Guid id)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .Include(s => s.Grades.Where(g => g.Status == GradeStatus.Approved))
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
            return null;

        var approvedGrades = student.Grades.Where(g => g.Status == GradeStatus.Approved).ToList();
        var totalAttendances = student.Attendances.Count;
        var presentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present);

        return new StudentDetailDto
        {
            Id = student.Id,
            FullName = $"{student.User.FirstName} {student.User.LastName}",
            FirstName = student.User.FirstName,
            LastName = student.User.LastName,
            Email = student.User.Email ?? "",
            StudentNumber = student.StudentNumber,
            ProfileImageUrl = student.User.ProfileImageUrl,

            GroupId = student.GroupId,
            GroupName = student.Group?.Name ?? "Neasignat",
            ProgramName = student.Group?.Program.Name ?? "N/A",
            FacultyName = student.Group?.Program.Faculty.Name ?? "N/A",
            Year = student.Group?.Year ?? 0,

            Status = student.Status,
            ScholarshipHolder = student.ScholarshipHolder,
            EnrollmentDate = student.EnrollmentDate,

            GradesCount = approvedGrades.Count,
            AverageGrade = approvedGrades.Any() ? approvedGrades.Average(g => g.Value) : null,
            AttendanceRate = totalAttendances > 0 ? Math.Round((decimal)presentCount / totalAttendances * 100, 1) : 0,
            TotalCredits = 0 // Will be calculated from completed courses
        };
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateStudentDto dto)
    {
        try
        {
            // Verify group exists
            var group = await _context.Groups.FindAsync(dto.GroupId);
            if (group == null)
            {
                return ServiceResult<Guid>.Failed("Grupa selectată nu există.");
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResult<Guid>.Failed("Email-ul este deja folosit.");
            }

            // Create user account
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Student,
                IsActive = true,
                EmailConfirmed = true
            };

            var defaultPassword = "Student123!"; // In production, generate and send via email
            var createResult = await _userManager.CreateAsync(user, defaultPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create user for student: {Errors}", errors);
                return ServiceResult<Guid>.Failed($"Eroare la crearea contului: {errors}");
            }

            // Generate unique student number
            var studentNumber = await GenerateStudentNumberAsync();

            // Create student record
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GroupId = dto.GroupId,
                StudentNumber = studentNumber,
                Status = StudentStatus.Active,
                EnrollmentDate = dto.EnrollmentDate,
                ScholarshipHolder = dto.ScholarshipHolder
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student created successfully: {StudentId} - {Email}", student.Id, dto.Email);

            return ServiceResult<Guid>.Success(student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return ServiceResult<Guid>.Failed("Eroare la crearea studentului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateStudentDto dto)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return ServiceResult.Failed("Studentul nu a fost găsit.");
            }

            // Verify group exists if changing
            if (dto.GroupId.HasValue && dto.GroupId != student.GroupId)
            {
                var group = await _context.Groups.FindAsync(dto.GroupId);
                if (group == null)
                {
                    return ServiceResult.Failed("Grupa selectată nu există.");
                }
            }

            // Update user info
            student.User.FirstName = dto.FirstName;
            student.User.LastName = dto.LastName;

            // Update student info
            student.GroupId = dto.GroupId;
            student.ScholarshipHolder = dto.ScholarshipHolder;
            student.Status = dto.Status;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Student updated successfully: {StudentId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", id);
            return ServiceResult.Failed("Eroare la actualizarea studentului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return ServiceResult.Failed("Studentul nu a fost găsit.");
            }

            // Soft delete - set status to Inactive
            student.Status = StudentStatus.Inactive;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Student soft deleted: {StudentId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return ServiceResult.Failed("Eroare la ștergerea studentului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> TransferGroupAsync(Guid studentId, Guid newGroupId)
    {
        try
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return ServiceResult.Failed("Studentul nu a fost găsit.");
            }

            var newGroup = await _context.Groups.FindAsync(newGroupId);
            if (newGroup == null)
            {
                return ServiceResult.Failed("Grupa destinație nu există.");
            }

            student.GroupId = newGroupId;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} transferred to group {GroupId}", studentId, newGroupId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring student {StudentId} to group {GroupId}", studentId, newGroupId);
            return ServiceResult.Failed("Eroare la transferul studentului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<List<StudentListDto>> ExportAsync(StudentFilter filter)
    {
        var query = _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
            .AsQueryable();

        // Apply same filters as GetAllAsync
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(s =>
                s.User.FirstName.ToLower().Contains(searchLower) ||
                s.User.LastName.ToLower().Contains(searchLower) ||
                s.User.Email.ToLower().Contains(searchLower) ||
                s.StudentNumber.ToLower().Contains(searchLower));
        }

        if (filter.GroupId.HasValue)
        {
            query = query.Where(s => s.GroupId == filter.GroupId);
        }

        if (filter.ProgramId.HasValue)
        {
            query = query.Where(s => s.Group!.ProgramId == filter.ProgramId);
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(s => s.Group!.Program.FacultyId == filter.FacultyId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(s => s.Status == filter.Status);
        }

        return await query
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email ?? "",
                StudentNumber = s.StudentNumber,
                GroupName = s.Group != null ? s.Group.Name : "Neasignat",
                ProgramName = s.Group != null ? s.Group.Program.Name : "N/A",
                Status = s.Status
            })
            .ToListAsync();
    }

    private async Task<string> GenerateStudentNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"{year}";

        var lastStudent = await _context.Students
            .Where(s => s.StudentNumber.StartsWith(prefix))
            .OrderByDescending(s => s.StudentNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastStudent != null && int.TryParse(lastStudent.StudentNumber.Substring(4), out int lastNumber))
        {
            nextNumber = lastNumber + 1;
        }

        return $"{prefix}{nextNumber:D6}";
    }

    public async Task<List<StudentListDto>> GetRecentAsync(int count = 10)
    {
        return await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email ?? "",
                StudentNumber = s.StudentNumber,
                GroupName = s.Group != null ? s.Group.Name : "Neasignat",
                ProgramName = s.Group != null ? s.Group.Program.Name : "N/A",
                Status = s.Status
            })
            .ToListAsync();
    }
}
