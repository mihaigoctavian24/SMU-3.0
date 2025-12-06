using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service for managing grade operations
/// </summary>
public class GradeService : IGradeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GradeService> _logger;

    public GradeService(ApplicationDbContext context, ILogger<GradeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GradeListDto>> GetByStudentAsync(Guid studentId)
    {
        return await _context.Grades
            .Where(g => g.StudentId == studentId)
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p.User)
            .Include(g => g.EnteredBy)
            .OrderByDescending(g => g.ExamDate)
            .Select(g => new GradeListDto
            {
                Id = g.Id,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseName = g.Course.Name,
                ProfessorName = g.Course.Professor != null ? $"{g.Course.Professor.User.FirstName} {g.Course.Professor.User.LastName}" : "",
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                Credits = g.Course.Credits,
                Semester = g.Course.Semester
            })
            .ToListAsync();
    }

    public async Task<List<GradeListDto>> GetByCourseAsync(Guid courseId)
    {
        return await _context.Grades
            .Where(g => g.CourseId == courseId)
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p.User)
            .Include(g => g.EnteredBy)
            .OrderBy(g => g.Student.User.LastName)
                .ThenBy(g => g.Student.User.FirstName)
            .Select(g => new GradeListDto
            {
                Id = g.Id,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseName = g.Course.Name,
                ProfessorName = g.Course.Professor != null ? $"{g.Course.Professor.User.FirstName} {g.Course.Professor.User.LastName}" : "",
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                Credits = g.Course.Credits,
                Semester = g.Course.Semester
            })
            .ToListAsync();
    }

    public async Task<List<GradeListDto>> GetPendingForApprovalAsync(Guid? facultyId = null)
    {
        var query = _context.Grades
            .Where(g => g.Status == GradeStatus.Pending)
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Program)
                .ThenInclude(p => p.Faculty)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p.User)
            .Include(g => g.EnteredBy)
            .AsQueryable();

        if (facultyId.HasValue)
        {
            query = query.Where(g => g.Course.Program.FacultyId == facultyId.Value);
        }

        return await query
            .OrderBy(g => g.ExamDate)
            .Select(g => new GradeListDto
            {
                Id = g.Id,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseName = g.Course.Name,
                ProfessorName = g.Course.Professor != null ? $"{g.Course.Professor.User.FirstName} {g.Course.Professor.User.LastName}" : "",
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                Credits = g.Course.Credits,
                Semester = g.Course.Semester
            })
            .ToListAsync();
    }

    public async Task<GradeDetailDto?> GetByIdAsync(Guid id)
    {
        return await _context.Grades
            .Where(g => g.Id == id)
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
            .Include(g => g.EnteredBy)
            .Select(g => new GradeDetailDto
            {
                Id = g.Id,
                StudentId = g.StudentId,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseId = g.CourseId,
                CourseName = g.Course.Name,
                CourseCode = g.Course.Code,
                Credits = g.Course.Credits,
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                Notes = g.Notes,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GradeResult> CreateAsync(CreateGradeDto dto, Guid enteredById)
    {
        // Validate student exists
        var studentExists = await _context.Students.AnyAsync(s => s.Id == dto.StudentId);
        if (!studentExists)
        {
            _logger.LogWarning("Attempted to create grade for non-existent student: {StudentId}", dto.StudentId);
            return GradeResult.Failed("Studentul nu există.");
        }

        // Validate course exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            _logger.LogWarning("Attempted to create grade for non-existent course: {CourseId}", dto.CourseId);
            return GradeResult.Failed("Cursul nu există.");
        }

        // Validate grade value
        if (dto.Value < 1 || dto.Value > 10)
        {
            return GradeResult.Failed("Nota trebuie să fie între 1 și 10.");
        }

        // Check for duplicate grade (same student, course, and type)
        var duplicateExists = await _context.Grades
            .AnyAsync(g => g.StudentId == dto.StudentId &&
                          g.CourseId == dto.CourseId &&
                          g.Type == dto.Type &&
                          g.Status != GradeStatus.Rejected);

        if (duplicateExists)
        {
            return GradeResult.Failed($"Există deja o notă de tip {dto.Type} pentru acest student la acest curs.");
        }

        var grade = new Grade
        {
            Id = Guid.NewGuid(),
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            Value = dto.Value,
            Type = dto.Type,
            Status = GradeStatus.Pending,
            ExamDate = dto.ExamDate,
            Notes = dto.Notes,
            EnteredById = enteredById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Grade created: {GradeId} by user {UserId}", grade.Id, enteredById);
        return GradeResult.Success(grade.Id);
    }

    public async Task<GradeResult> UpdateAsync(Guid id, UpdateGradeDto dto)
    {
        var grade = await _context.Grades.FindAsync(id);

        if (grade == null)
        {
            return GradeResult.Failed("Nota nu a fost găsită.");
        }

        if (grade.Status != GradeStatus.Pending)
        {
            return GradeResult.Failed("Doar notele cu status Pending pot fi modificate.");
        }

        // Validate grade value
        if (dto.Value < 1 || dto.Value > 10)
        {
            return GradeResult.Failed("Nota trebuie să fie între 1 și 10.");
        }

        grade.Value = dto.Value;
        grade.Type = dto.Type;
        grade.ExamDate = dto.ExamDate;
        grade.Notes = dto.Notes;
        grade.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Grade updated: {GradeId}", id);
        return GradeResult.Success(id);
    }

    public async Task<GradeResult> ApproveAsync(Guid id, Guid approvedById)
    {
        var grade = await _context.Grades.FindAsync(id);

        if (grade == null)
        {
            return GradeResult.Failed("Nota nu a fost găsită.");
        }

        if (grade.Status != GradeStatus.Pending)
        {
            return GradeResult.Failed("Doar notele cu status Pending pot fi aprobate.");
        }

        grade.Status = GradeStatus.Approved;
        grade.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Grade approved: {GradeId} by user {UserId}", id, approvedById);
        return GradeResult.Success(id);
    }

    public async Task<GradeResult> RejectAsync(Guid id, Guid rejectedById, string reason)
    {
        var grade = await _context.Grades.FindAsync(id);

        if (grade == null)
        {
            return GradeResult.Failed("Nota nu a fost găsită.");
        }

        if (grade.Status != GradeStatus.Pending)
        {
            return GradeResult.Failed("Doar notele cu status Pending pot fi respinse.");
        }

        grade.Status = GradeStatus.Rejected;
        grade.Notes = string.IsNullOrEmpty(grade.Notes)
            ? $"Respins: {reason}"
            : $"{grade.Notes}\nRespins: {reason}";
        grade.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Grade rejected: {GradeId} by user {UserId}, reason: {Reason}", id, rejectedById, reason);
        return GradeResult.Success(id);
    }

    public async Task<GradeResult> BulkCreateAsync(List<CreateGradeDto> grades, Guid enteredById)
    {
        if (grades == null || grades.Count == 0)
        {
            return GradeResult.Failed("Lista de note este goală.");
        }

        var gradeEntities = new List<Grade>();

        foreach (var dto in grades)
        {
            // Validate grade value
            if (dto.Value < 1 || dto.Value > 10)
            {
                return GradeResult.Failed($"Nota trebuie să fie între 1 și 10 (student: {dto.StudentId}).");
            }

            // Check for duplicate
            var duplicateExists = await _context.Grades
                .AnyAsync(g => g.StudentId == dto.StudentId &&
                              g.CourseId == dto.CourseId &&
                              g.Type == dto.Type &&
                              g.Status != GradeStatus.Rejected);

            if (duplicateExists)
            {
                continue; // Skip duplicates
            }

            gradeEntities.Add(new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                Value = dto.Value,
                Type = dto.Type,
                Status = GradeStatus.Pending,
                ExamDate = dto.ExamDate,
                Notes = dto.Notes,
                EnteredById = enteredById,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Grades.AddRange(gradeEntities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk grade creation: {Count} grades created by user {UserId}", gradeEntities.Count, enteredById);
        return GradeResult.Success();
    }

    public async Task<StudentAverageDto?> GetStudentAverageAsync(Guid studentId)
    {
        var approvedGrades = await _context.Grades
            .Where(g => g.StudentId == studentId && g.Status == GradeStatus.Approved)
            .Include(g => g.Course)
            .ToListAsync();

        if (approvedGrades.Count == 0)
        {
            return new StudentAverageDto
            {
                StudentId = studentId,
                WeightedAverage = 0,
                TotalCredits = 0,
                CompletedCourses = 0,
                PassedGrades = 0,
                FailedGrades = 0
            };
        }

        // Calculate weighted average: sum(grade * credits) / sum(credits)
        var totalWeightedGrades = approvedGrades.Sum(g => g.Value * g.Course.Credits);
        var totalCredits = approvedGrades.Sum(g => g.Course.Credits);

        return new StudentAverageDto
        {
            StudentId = studentId,
            WeightedAverage = totalCredits > 0 ? totalWeightedGrades / totalCredits : 0,
            TotalCredits = totalCredits,
            CompletedCourses = approvedGrades.Select(g => g.CourseId).Distinct().Count(),
            PassedGrades = approvedGrades.Count(g => g.Value >= 5),
            FailedGrades = approvedGrades.Count(g => g.Value < 5)
        };
    }

    public async Task<GradeResult> DeleteAsync(Guid id)
    {
        var grade = await _context.Grades.FindAsync(id);

        if (grade == null)
        {
            return GradeResult.Failed("Nota nu a fost găsită.");
        }

        if (grade.Status != GradeStatus.Pending)
        {
            return GradeResult.Failed("Doar notele cu status Pending pot fi șterse.");
        }

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Grade deleted: {GradeId}", id);
        return GradeResult.Success();
    }

    public async Task<List<GradeListDto>> GetFilteredGradesAsync(GradeFilterDto filter)
    {
        var query = _context.Grades
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p.User)
            .Include(g => g.EnteredBy)
            .AsQueryable();

        // Apply filters
        if (filter.StudentId.HasValue)
            query = query.Where(g => g.StudentId == filter.StudentId.Value);

        if (filter.CourseId.HasValue)
            query = query.Where(g => g.CourseId == filter.CourseId.Value);

        if (filter.ProfessorId.HasValue)
            query = query.Where(g => g.Course.ProfessorId == filter.ProfessorId.Value);

        if (filter.Semester.HasValue)
            query = query.Where(g => g.Course.Semester == filter.Semester.Value);

        if (filter.Status.HasValue)
            query = query.Where(g => g.Status == filter.Status.Value);

        if (filter.Type.HasValue)
            query = query.Where(g => g.Type == filter.Type.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(g => g.ExamDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(g => g.ExamDate <= filter.ToDate.Value);

        return await query
            .OrderByDescending(g => g.ExamDate)
            .Select(g => new GradeListDto
            {
                Id = g.Id,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseName = g.Course.Name,
                ProfessorName = g.Course.Professor != null ? $"{g.Course.Professor.User.FirstName} {g.Course.Professor.User.LastName}" : "",
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                Credits = g.Course.Credits,
                Semester = g.Course.Semester
            })
            .ToListAsync();
    }

    public async Task<GradeStatisticsDto> GetStatisticsAsync(Guid? facultyId = null)
    {
        var query = _context.Grades.AsQueryable();

        if (facultyId.HasValue)
        {
            query = query
                .Include(g => g.Course)
                    .ThenInclude(c => c.Program)
                .Where(g => g.Course.Program.FacultyId == facultyId.Value);
        }

        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var grades = await query.ToListAsync();

        var stats = new GradeStatisticsDto
        {
            TotalGrades = grades.Count,
            PendingGrades = grades.Count(g => g.Status == GradeStatus.Pending),
            ApprovedToday = grades.Count(g => g.Status == GradeStatus.Approved && g.UpdatedAt.Date == today),
            ApprovedThisWeek = grades.Count(g => g.Status == GradeStatus.Approved && g.UpdatedAt.Date >= weekAgo),
            AverageGrade = grades.Any(g => g.Status == GradeStatus.Approved)
                ? grades.Where(g => g.Status == GradeStatus.Approved).Average(g => g.Value)
                : 0,
            FailingGrades = grades.Count(g => g.Status == GradeStatus.Approved && g.Value < 5),
            GradesByType = grades
                .GroupBy(g => g.Type)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task<List<GradeListDto>> GetFailingGradesAsync(Guid? facultyId = null)
    {
        var query = _context.Grades
            .Where(g => g.Status == GradeStatus.Approved && g.Value < 5)
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p.User)
            .Include(g => g.EnteredBy)
            .AsQueryable();

        if (facultyId.HasValue)
        {
            query = query
                .Include(g => g.Course)
                    .ThenInclude(c => c.Program)
                .Where(g => g.Course.Program.FacultyId == facultyId.Value);
        }

        return await query
            .OrderBy(g => g.Value)
            .Select(g => new GradeListDto
            {
                Id = g.Id,
                StudentName = $"{g.Student.User.FirstName} {g.Student.User.LastName}",
                StudentNumber = g.Student.StudentNumber,
                CourseName = g.Course.Name,
                ProfessorName = g.Course.Professor != null ? $"{g.Course.Professor.User.FirstName} {g.Course.Professor.User.LastName}" : "",
                Value = g.Value,
                Type = g.Type,
                Status = g.Status,
                ExamDate = g.ExamDate,
                EnteredByName = g.EnteredBy != null ? $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}" : null,
                Credits = g.Course.Credits,
                Semester = g.Course.Semester
            })
            .ToListAsync();
    }

    public async Task<List<MissingGradeDto>> GetMissingGradesAsync(Guid courseId)
    {
        // Get all students enrolled in the course
        var courseStudents = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g.Program)
                    .ThenInclude(p => p.Courses)
            .Where(s => s.Group.Program.Courses.Any(c => c.Id == courseId))
            .ToListAsync();

        // Get existing grades for the course
        var existingGrades = await _context.Grades
            .Where(g => g.CourseId == courseId && g.Status != GradeStatus.Rejected)
            .Select(g => g.StudentId)
            .Distinct()
            .ToListAsync();

        // Find students without grades
        var studentsWithoutGrades = courseStudents
            .Where(s => !existingGrades.Contains(s.Id))
            .ToList();

        var course = await _context.Courses.FindAsync(courseId);

        return studentsWithoutGrades.Select(s => new MissingGradeDto
        {
            StudentId = s.Id,
            StudentName = $"{s.User.FirstName} {s.User.LastName}",
            StudentNumber = s.StudentNumber,
            CourseId = courseId,
            CourseName = course?.Name ?? ""
        }).ToList();
    }

    public async Task<List<GradeHistoryDto>> GetGradeHistoryAsync(Guid gradeId)
    {
        var grade = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
            .Include(g => g.EnteredBy)
            .FirstOrDefaultAsync(g => g.Id == gradeId);

        if (grade == null)
        {
            return new List<GradeHistoryDto>();
        }

        var history = new List<GradeHistoryDto>();

        // Created event
        history.Add(new GradeHistoryDto
        {
            GradeId = grade.Id,
            Action = "Created",
            Timestamp = grade.CreatedAt,
            PerformedBy = grade.EnteredBy != null
                ? $"{grade.EnteredBy.FirstName} {grade.EnteredBy.LastName}"
                : "Necunoscut",
            Description = $"Nota a fost introdusă pentru {grade.Student.User.FirstName} {grade.Student.User.LastName} la disciplina {grade.Course.Name}",
            NewValue = grade.Value.ToString("0.00"),
            Notes = grade.Notes
        });

        // Status changes
        if (grade.Status == GradeStatus.Approved)
        {
            history.Add(new GradeHistoryDto
            {
                GradeId = grade.Id,
                Action = "Approved",
                Timestamp = grade.UpdatedAt,
                PerformedBy = grade.EnteredBy != null
                    ? $"{grade.EnteredBy.FirstName} {grade.EnteredBy.LastName}"
                    : "Necunoscut",
                Description = "Nota a fost aprobată",
                OldValue = "În așteptare",
                NewValue = "Aprobat"
            });
        }
        else if (grade.Status == GradeStatus.Rejected)
        {
            history.Add(new GradeHistoryDto
            {
                GradeId = grade.Id,
                Action = "Rejected",
                Timestamp = grade.UpdatedAt,
                PerformedBy = grade.EnteredBy != null
                    ? $"{grade.EnteredBy.FirstName} {grade.EnteredBy.LastName}"
                    : "Necunoscut",
                Description = "Nota a fost respinsă",
                OldValue = "În așteptare",
                NewValue = "Respins",
                Notes = grade.Notes
            });
        }

        // If updated after creation
        if (grade.UpdatedAt > grade.CreatedAt.AddSeconds(1) && grade.Status == GradeStatus.Pending)
        {
            history.Add(new GradeHistoryDto
            {
                GradeId = grade.Id,
                Action = "Updated",
                Timestamp = grade.UpdatedAt,
                PerformedBy = grade.EnteredBy != null
                    ? $"{grade.EnteredBy.FirstName} {grade.EnteredBy.LastName}"
                    : "Necunoscut",
                Description = "Nota a fost modificată",
                Notes = grade.Notes
            });
        }

        // Sort by timestamp descending (most recent first)
        return history.OrderByDescending(h => h.Timestamp).ToList();
    }
}
