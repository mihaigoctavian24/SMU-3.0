using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Models;

namespace SMU.Services;

public interface ICourseService
{
    Task<List<CourseListDto>> GetAllAsync(CourseFilter filter);
    Task<CourseDetailDto?> GetByIdAsync(Guid id);
    Task<List<CourseListDto>> GetByProfessorAsync(Guid professorId);
    Task<List<CourseListDto>> GetByProgramAsync(Guid programId);
    Task<ServiceResult<Guid>> CreateAsync(CreateCourseDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateCourseDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult> AssignProfessorAsync(Guid courseId, Guid professorId);
    Task<CourseStatsDto> GetCourseStatsAsync(Guid courseId);
    Task<List<ProgramOption>> GetProgramOptionsAsync();
    Task<List<ProfessorOption>> GetProfessorOptionsAsync();
}

public class CourseService : ICourseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CourseService> _logger;

    public CourseService(ApplicationDbContext context, ILogger<CourseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CourseListDto>> GetAllAsync(CourseFilter filter)
    {
        var query = _context.Courses
            .Include(c => c.Professor)
                .ThenInclude(p => p.User)
            .Include(c => c.Program)
            .AsQueryable();

        // Apply filters
        if (filter.ProgramId.HasValue)
            query = query.Where(c => c.ProgramId == filter.ProgramId.Value);

        if (filter.ProfessorId.HasValue)
            query = query.Where(c => c.ProfessorId == filter.ProfessorId.Value);

        if (filter.Year.HasValue)
            query = query.Where(c => c.Year == filter.Year.Value);

        if (filter.Semester.HasValue)
            query = query.Where(c => c.Semester == filter.Semester.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(c => c.IsActive == filter.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchLower) ||
                c.Code.ToLower().Contains(searchLower));
        }

        var courses = await query
            .OrderBy(c => c.Year)
            .ThenBy(c => c.Semester)
            .ThenBy(c => c.Name)
            .ToListAsync();

        // Get student counts
        var courseIds = courses.Select(c => c.Id).ToList();
        var studentCounts = await _context.Grades
            .Where(g => courseIds.Contains(g.CourseId))
            .GroupBy(g => g.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Select(x => x.StudentId).Distinct().Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        return courses.Select(c => new CourseListDto
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            ProfessorName = c.Professor != null
                ? $"{c.Professor.User.FirstName} {c.Professor.User.LastName}"
                : "Neasignat",
            ProgramName = c.Program.Name,
            Credits = c.Credits,
            Year = c.Year,
            Semester = c.Semester,
            StudentsCount = studentCounts.ContainsKey(c.Id) ? studentCounts[c.Id] : 0,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<CourseDetailDto?> GetByIdAsync(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Professor)
                .ThenInclude(p => p.User)
            .Include(c => c.Program)
                .ThenInclude(p => p.Faculty)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return null;

        var stats = await GetCourseStatsAsync(id);

        var recentGrades = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Where(g => g.CourseId == id)
            .OrderByDescending(g => g.CreatedAt)
            .Take(10)
            .Select(g => new RecentGradeDto
            {
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                Value = g.Value,
                Type = g.Type.ToString(),
                ExamDate = g.ExamDate,
                Status = g.Status.ToString()
            })
            .ToListAsync();

        return new CourseDetailDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            ProfessorName = course.Professor != null
                ? $"{course.Professor.User.FirstName} {course.Professor.User.LastName}"
                : "Neasignat",
            ProfessorId = course.ProfessorId,
            ProgramName = course.Program.Name,
            ProgramId = course.ProgramId,
            FacultyName = course.Program.Faculty.Name,
            Credits = course.Credits,
            Year = course.Year,
            Semester = course.Semester,
            Description = course.Description,
            IsActive = course.IsActive,
            Stats = stats,
            RecentGrades = recentGrades
        };
    }

    public async Task<List<CourseListDto>> GetByProfessorAsync(Guid professorId)
    {
        return await GetAllAsync(new CourseFilter { ProfessorId = professorId, IsActive = true });
    }

    public async Task<List<CourseListDto>> GetByProgramAsync(Guid programId)
    {
        return await GetAllAsync(new CourseFilter { ProgramId = programId, IsActive = true });
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateCourseDto dto)
    {
        try
        {
            // Check if code already exists
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == dto.Code);

            if (existingCourse != null)
            {
                return ServiceResult<Guid>.Failed("Un curs cu acest cod există deja.");
            }

            // Validate program exists
            var programExists = await _context.Programs
                .AnyAsync(p => p.Id == dto.ProgramId && p.IsActive);

            if (!programExists)
            {
                return ServiceResult<Guid>.Failed("Programul de studii selectat nu există sau este inactiv.");
            }

            // Validate professor if provided
            if (dto.ProfessorId.HasValue)
            {
                var professorExists = await _context.Professors
                    .AnyAsync(p => p.Id == dto.ProfessorId.Value);

                if (!professorExists)
                {
                    return ServiceResult<Guid>.Failed("Profesorul selectat nu există.");
                }
            }

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                ProgramId = dto.ProgramId,
                ProfessorId = dto.ProfessorId,
                Credits = dto.Credits,
                Year = dto.Year,
                Semester = dto.Semester,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Course created: {CourseId} - {CourseName}", course.Id, course.Name);

            return ServiceResult<Guid>.Success(course.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return ServiceResult<Guid>.Failed("A apărut o eroare la crearea cursului.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateCourseDto dto)
    {
        try
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return ServiceResult.Failed("Cursul nu a fost găsit.");
            }

            // Check if code already exists (excluding current course)
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == dto.Code && c.Id != id);

            if (existingCourse != null)
            {
                return ServiceResult.Failed("Un alt curs cu acest cod există deja.");
            }

            // Validate professor if provided
            if (dto.ProfessorId.HasValue)
            {
                var professorExists = await _context.Professors
                    .AnyAsync(p => p.Id == dto.ProfessorId.Value);

                if (!professorExists)
                {
                    return ServiceResult.Failed("Profesorul selectat nu există.");
                }
            }

            course.Name = dto.Name;
            course.Code = dto.Code;
            course.ProfessorId = dto.ProfessorId;
            course.Credits = dto.Credits;
            course.Description = dto.Description;
            course.IsActive = dto.IsActive;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Course updated: {CourseId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId}", id);
            return ServiceResult.Failed("A apărut o eroare la actualizarea cursului.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return ServiceResult.Failed("Cursul nu a fost găsit.");
            }

            // Soft delete - set IsActive to false
            course.IsActive = false;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Course soft deleted: {CourseId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course {CourseId}", id);
            return ServiceResult.Failed("A apărut o eroare la ștergerea cursului.");
        }
    }

    public async Task<ServiceResult> AssignProfessorAsync(Guid courseId, Guid professorId)
    {
        try
        {
            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
            {
                return ServiceResult.Failed("Cursul nu a fost găsit.");
            }

            var professorExists = await _context.Professors
                .AnyAsync(p => p.Id == professorId);

            if (!professorExists)
            {
                return ServiceResult.Failed("Profesorul nu a fost găsit.");
            }

            course.ProfessorId = professorId;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Professor {ProfessorId} assigned to course {CourseId}", professorId, courseId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning professor to course {CourseId}", courseId);
            return ServiceResult.Failed("A apărut o eroare la asignarea profesorului.");
        }
    }

    public async Task<CourseStatsDto> GetCourseStatsAsync(Guid courseId)
    {
        var grades = await _context.Grades
            .Where(g => g.CourseId == courseId && g.Status == GradeStatus.Approved)
            .ToListAsync();

        var enrolledStudents = grades
            .Select(g => g.StudentId)
            .Distinct()
            .Count();

        var averageGrade = grades.Any()
            ? grades.Average(g => g.Value)
            : 0;

        var passedStudents = grades
            .Where(g => g.Value >= 5.0m && g.Type == GradeType.Final)
            .Select(g => g.StudentId)
            .Distinct()
            .Count();

        var totalFinalGrades = grades
            .Where(g => g.Type == GradeType.Final)
            .Select(g => g.StudentId)
            .Distinct()
            .Count();

        var passRate = totalFinalGrades > 0
            ? (decimal)passedStudents / totalFinalGrades * 100
            : 0;

        var totalClasses = await _context.ScheduleEntries
            .Where(s => s.CourseId == courseId)
            .CountAsync();

        return new CourseStatsDto
        {
            EnrolledStudents = enrolledStudents,
            AverageGrade = averageGrade,
            PassRate = passRate,
            GradesCount = grades.Count,
            TotalClasses = totalClasses
        };
    }

    public async Task<List<ProgramOption>> GetProgramOptionsAsync()
    {
        return await _context.Programs
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProgramOption
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code
            })
            .ToListAsync();
    }

    public async Task<List<ProfessorOption>> GetProfessorOptionsAsync()
    {
        return await _context.Professors
            .Include(p => p.User)
            .OrderBy(p => p.User.LastName)
            .ThenBy(p => p.User.FirstName)
            .Select(p => new ProfessorOption
            {
                Id = p.Id,
                FullName = $"{p.User.FirstName} {p.User.LastName}",
                Title = p.Title
            })
            .ToListAsync();
    }
}

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failed(string error) => new() { Succeeded = false, ErrorMessage = error };
}

public class ServiceResult<T>
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public T? Data { get; private set; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static ServiceResult<T> Failed(string error) => new() { Succeeded = false, ErrorMessage = error };
}
