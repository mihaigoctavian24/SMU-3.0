using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMU.Data.Entities;

namespace SMU.Data;

/// <summary>
/// Main database context for SMU application
/// Connects to Supabase PostgreSQL with existing schema
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid,
    IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Professor> Professors => Set<Professor>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables to use existing Supabase schema
        ConfigureIdentityTables(builder);

        // Configure core entities
        ConfigureCoreTables(builder);

        // Configure PostgreSQL ENUMs
        ConfigureEnums(builder);
    }

    private void ConfigureIdentityTables(ModelBuilder builder)
    {
        // Map Identity tables to snake_case names in Supabase
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("asp_net_users");
            b.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100);
            b.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100);
            b.Property(u => u.Role).HasColumnName("role");
            b.Property(u => u.IsActive).HasColumnName("is_active");
            b.Property(u => u.CreatedAt).HasColumnName("created_at");
            b.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            b.Property(u => u.ProfileImageUrl).HasColumnName("profile_image_url");

            // Map standard Identity columns
            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.UserName).HasColumnName("user_name");
            b.Property(u => u.NormalizedUserName).HasColumnName("normalized_user_name");
            b.Property(u => u.Email).HasColumnName("email");
            b.Property(u => u.NormalizedEmail).HasColumnName("normalized_email");
            b.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
            b.Property(u => u.PasswordHash).HasColumnName("password_hash");
            b.Property(u => u.SecurityStamp).HasColumnName("security_stamp");
            b.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            b.Property(u => u.PhoneNumber).HasColumnName("phone_number");
            b.Property(u => u.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            b.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            b.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
            b.Property(u => u.LockoutEnabled).HasColumnName("lockout_enabled");
            b.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("asp_net_roles");
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.Name).HasColumnName("name");
            b.Property(r => r.NormalizedName).HasColumnName("normalized_name");
            b.Property(r => r.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            b.Property(r => r.Description).HasColumnName("description");
            b.Property(r => r.CreatedAt).HasColumnName("created_at");
        });

        builder.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("asp_net_user_roles");
            b.Property(ur => ur.UserId).HasColumnName("user_id");
            b.Property(ur => ur.RoleId).HasColumnName("role_id");
        });

        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("asp_net_user_claims");
            b.Property(uc => uc.Id).HasColumnName("id");
            b.Property(uc => uc.UserId).HasColumnName("user_id");
            b.Property(uc => uc.ClaimType).HasColumnName("claim_type");
            b.Property(uc => uc.ClaimValue).HasColumnName("claim_value");
        });

        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("asp_net_user_logins");
            b.Property(ul => ul.LoginProvider).HasColumnName("login_provider");
            b.Property(ul => ul.ProviderKey).HasColumnName("provider_key");
            b.Property(ul => ul.ProviderDisplayName).HasColumnName("provider_display_name");
            b.Property(ul => ul.UserId).HasColumnName("user_id");
        });

        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("asp_net_user_tokens");
            b.Property(ut => ut.UserId).HasColumnName("user_id");
            b.Property(ut => ut.LoginProvider).HasColumnName("login_provider");
            b.Property(ut => ut.Name).HasColumnName("name");
            b.Property(ut => ut.Value).HasColumnName("value");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("asp_net_role_claims");
            b.Property(rc => rc.Id).HasColumnName("id");
            b.Property(rc => rc.RoleId).HasColumnName("role_id");
            b.Property(rc => rc.ClaimType).HasColumnName("claim_type");
            b.Property(rc => rc.ClaimValue).HasColumnName("claim_value");
        });
    }

    private void ConfigureCoreTables(ModelBuilder builder)
    {
        // Faculty
        builder.Entity<Faculty>(b =>
        {
            b.ToTable("faculties");
            b.HasKey(f => f.Id);
            b.Property(f => f.Id).HasColumnName("id");
            b.Property(f => f.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(f => f.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            b.Property(f => f.Description).HasColumnName("description");
            b.Property(f => f.DeanId).HasColumnName("dean_id");
            b.Property(f => f.CreatedAt).HasColumnName("created_at");
            b.Property(f => f.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(f => f.Dean)
                .WithMany()
                .HasForeignKey(f => f.DeanId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(f => f.Code).IsUnique();
        });

        // Program
        builder.Entity<Program>(b =>
        {
            b.ToTable("programs");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id");
            b.Property(p => p.FacultyId).HasColumnName("faculty_id").IsRequired();
            b.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(p => p.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            b.Property(p => p.Type).HasColumnName("type");
            b.Property(p => p.DurationYears).HasColumnName("duration_years");
            b.Property(p => p.TotalCredits).HasColumnName("total_credits");
            b.Property(p => p.Description).HasColumnName("description");
            b.Property(p => p.CreatedAt).HasColumnName("created_at");
            b.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(p => p.Faculty)
                .WithMany(f => f.Programs)
                .HasForeignKey(p => p.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(p => p.Code).IsUnique();
        });

        // Group
        builder.Entity<Group>(b =>
        {
            b.ToTable("groups");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).HasColumnName("id");
            b.Property(g => g.ProgramId).HasColumnName("program_id").IsRequired();
            b.Property(g => g.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            b.Property(g => g.Year).HasColumnName("year");
            b.Property(g => g.AcademicYear).HasColumnName("academic_year").HasMaxLength(20);
            b.Property(g => g.MaxStudents).HasColumnName("max_students");
            b.Property(g => g.CreatedAt).HasColumnName("created_at");
            b.Property(g => g.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(g => g.Program)
                .WithMany(p => p.Groups)
                .HasForeignKey(g => g.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Student
        builder.Entity<Student>(b =>
        {
            b.ToTable("students");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            b.Property(s => s.GroupId).HasColumnName("group_id").IsRequired();
            b.Property(s => s.StudentNumber).HasColumnName("student_number").HasMaxLength(20).IsRequired();
            b.Property(s => s.CurrentYear).HasColumnName("current_year");
            b.Property(s => s.Status).HasColumnName("status");
            b.Property(s => s.EnrollmentDate).HasColumnName("enrollment_date");
            b.Property(s => s.GraduationDate).HasColumnName("graduation_date");
            b.Property(s => s.TotalCredits).HasColumnName("total_credits").HasPrecision(5, 2);
            b.Property(s => s.Gpa).HasColumnName("gpa").HasPrecision(3, 2);
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Group)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(s => s.StudentNumber).IsUnique();
        });

        // Professor
        builder.Entity<Professor>(b =>
        {
            b.ToTable("professors");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id");
            b.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
            b.Property(p => p.FacultyId).HasColumnName("faculty_id").IsRequired();
            b.Property(p => p.Title).HasColumnName("title").HasMaxLength(50);
            b.Property(p => p.Department).HasColumnName("department").HasMaxLength(100);
            b.Property(p => p.Office).HasColumnName("office").HasMaxLength(50);
            b.Property(p => p.Phone).HasColumnName("phone").HasMaxLength(20);
            b.Property(p => p.HireDate).HasColumnName("hire_date");
            b.Property(p => p.IsActive).HasColumnName("is_active");
            b.Property(p => p.CreatedAt).HasColumnName("created_at");
            b.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(p => p.User)
                .WithOne(u => u.Professor)
                .HasForeignKey<Professor>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Faculty)
                .WithMany(f => f.Professors)
                .HasForeignKey(p => p.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Course
        builder.Entity<Course>(b =>
        {
            b.ToTable("courses");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.ProgramId).HasColumnName("program_id").IsRequired();
            b.Property(c => c.ProfessorId).HasColumnName("professor_id").IsRequired();
            b.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(c => c.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            b.Property(c => c.Credits).HasColumnName("credits");
            b.Property(c => c.Semester).HasColumnName("semester");
            b.Property(c => c.Year).HasColumnName("year");
            b.Property(c => c.Description).HasColumnName("description");
            b.Property(c => c.Syllabus).HasColumnName("syllabus");
            b.Property(c => c.IsOptional).HasColumnName("is_optional");
            b.Property(c => c.MaxStudents).HasColumnName("max_students");
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(c => c.Program)
                .WithMany(p => p.Courses)
                .HasForeignKey(c => c.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.Professor)
                .WithMany(p => p.Courses)
                .HasForeignKey(c => c.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(c => c.Code).IsUnique();
        });

        // Grade
        builder.Entity<Grade>(b =>
        {
            b.ToTable("grades");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).HasColumnName("id");
            b.Property(g => g.StudentId).HasColumnName("student_id").IsRequired();
            b.Property(g => g.CourseId).HasColumnName("course_id").IsRequired();
            b.Property(g => g.ProfessorId).HasColumnName("professor_id").IsRequired();
            b.Property(g => g.Value).HasColumnName("value").HasPrecision(4, 2);
            b.Property(g => g.Type).HasColumnName("type");
            b.Property(g => g.Status).HasColumnName("status");
            b.Property(g => g.GradedAt).HasColumnName("graded_at");
            b.Property(g => g.Comments).HasColumnName("comments");
            b.Property(g => g.CreatedAt).HasColumnName("created_at");
            b.Property(g => g.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(g => g.Course)
                .WithMany(c => c.Grades)
                .HasForeignKey(g => g.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(g => g.Professor)
                .WithMany(p => p.GradesGiven)
                .HasForeignKey(g => g.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Attendance
        builder.Entity<Attendance>(b =>
        {
            b.ToTable("attendances");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id");
            b.Property(a => a.StudentId).HasColumnName("student_id").IsRequired();
            b.Property(a => a.CourseId).HasColumnName("course_id").IsRequired();
            b.Property(a => a.Date).HasColumnName("date");
            b.Property(a => a.Status).HasColumnName("status");
            b.Property(a => a.Notes).HasColumnName("notes");
            b.Property(a => a.MarkedById).HasColumnName("marked_by_id");
            b.Property(a => a.CreatedAt).HasColumnName("created_at");

            b.HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Course)
                .WithMany(c => c.Attendances)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.MarkedBy)
                .WithMany()
                .HasForeignKey(a => a.MarkedById)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(a => new { a.StudentId, a.CourseId, a.Date }).IsUnique();
        });

        // ScheduleEntry
        builder.Entity<ScheduleEntry>(b =>
        {
            b.ToTable("schedule_entries");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.CourseId).HasColumnName("course_id").IsRequired();
            b.Property(s => s.GroupId).HasColumnName("group_id").IsRequired();
            b.Property(s => s.DayOfWeek).HasColumnName("day_of_week");
            b.Property(s => s.StartTime).HasColumnName("start_time");
            b.Property(s => s.EndTime).HasColumnName("end_time");
            b.Property(s => s.Room).HasColumnName("room").HasMaxLength(50);
            b.Property(s => s.Building).HasColumnName("building").HasMaxLength(100);
            b.Property(s => s.Type).HasColumnName("type");
            b.Property(s => s.StartDate).HasColumnName("start_date");
            b.Property(s => s.EndDate).HasColumnName("end_date");
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(s => s.Course)
                .WithMany(c => c.ScheduleEntries)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Group)
                .WithMany(g => g.ScheduleEntries)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentRequest
        builder.Entity<DocumentRequest>(b =>
        {
            b.ToTable("document_requests");
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).HasColumnName("id");
            b.Property(d => d.StudentId).HasColumnName("student_id").IsRequired();
            b.Property(d => d.Type).HasColumnName("type");
            b.Property(d => d.Status).HasColumnName("status");
            b.Property(d => d.Description).HasColumnName("description");
            b.Property(d => d.Response).HasColumnName("response");
            b.Property(d => d.ProcessedById).HasColumnName("processed_by_id");
            b.Property(d => d.ProcessedAt).HasColumnName("processed_at");
            b.Property(d => d.CreatedAt).HasColumnName("created_at");
            b.Property(d => d.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(d => d.Student)
                .WithMany(s => s.DocumentRequests)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(d => d.ProcessedBy)
                .WithMany()
                .HasForeignKey(d => d.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Notification
        builder.Entity<Notification>(b =>
        {
            b.ToTable("notifications");
            b.HasKey(n => n.Id);
            b.Property(n => n.Id).HasColumnName("id");
            b.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
            b.Property(n => n.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            b.Property(n => n.Message).HasColumnName("message").IsRequired();
            b.Property(n => n.Type).HasColumnName("type");
            b.Property(n => n.IsRead).HasColumnName("is_read");
            b.Property(n => n.Link).HasColumnName("link").HasMaxLength(500);
            b.Property(n => n.CreatedAt).HasColumnName("created_at");

            b.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // AuditLog
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id");
            b.Property(a => a.UserId).HasColumnName("user_id");
            b.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
            b.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
            b.Property(a => a.EntityId).HasColumnName("entity_id");
            b.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
            b.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
            b.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            b.Property(a => a.UserAgent).HasColumnName("user_agent");
            b.Property(a => a.CreatedAt).HasColumnName("created_at");

            b.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(a => a.CreatedAt);
            b.HasIndex(a => new { a.EntityType, a.EntityId });
        });
    }

    private void ConfigureEnums(ModelBuilder builder)
    {
        // Configure PostgreSQL ENUMs
        builder.HasPostgresEnum<UserRole>();
        builder.HasPostgresEnum<ProgramType>();
        builder.HasPostgresEnum<StudentStatus>();
        builder.HasPostgresEnum<GradeType>();
        builder.HasPostgresEnum<GradeStatus>();
        builder.HasPostgresEnum<AttendanceStatus>();
        builder.HasPostgresEnum<NotificationType>();
        builder.HasPostgresEnum<RequestStatus>();
        builder.HasPostgresEnum<RequestType>();
        builder.HasPostgresEnum<RiskLevel>();
    }
}
