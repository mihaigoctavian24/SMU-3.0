using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Custom claims principal factory that adds the Role claim from ApplicationUser.Role
/// This enables the authorization policies to work with RequireClaim("Role", ...)
/// Also adds StudentId, ProfessorId, and FacultyId claims for dashboard context
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        ApplicationDbContext dbContext)
        : base(userManager, roleManager, optionsAccessor)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add custom Role claim from the user's Role property
        identity.AddClaim(new Claim("Role", user.Role.ToString()));

        // Add additional useful claims
        identity.AddClaim(new Claim("FullName", user.FullName));
        identity.AddClaim(new Claim("IsActive", user.IsActive.ToString()));

        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            identity.AddClaim(new Claim("ProfileImageUrl", user.ProfileImageUrl));
        }

        // Add StudentId claim if user is a student
        if (user.Role == UserRole.Student)
        {
            var student = await _dbContext.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student != null)
            {
                identity.AddClaim(new Claim("StudentId", student.Id.ToString()));

                // Also add GroupId if student has a group
                if (student.GroupId.HasValue)
                {
                    identity.AddClaim(new Claim("GroupId", student.GroupId.Value.ToString()));
                }
            }
        }

        // Add ProfessorId and FacultyId claims if user is a professor
        if (user.Role == UserRole.Professor)
        {
            var professor = await _dbContext.Professors
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (professor != null)
            {
                identity.AddClaim(new Claim("ProfessorId", professor.Id.ToString()));

                if (professor.FacultyId.HasValue)
                {
                    identity.AddClaim(new Claim("FacultyId", professor.FacultyId.Value.ToString()));
                }
            }
        }

        // Add FacultyId claim for Dean (they are linked via faculties.dean_id)
        if (user.Role == UserRole.Dean)
        {
            var faculty = await _dbContext.Faculties
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.DeanId == user.Id);

            if (faculty != null)
            {
                identity.AddClaim(new Claim("FacultyId", faculty.Id.ToString()));
            }
        }

        // Add FacultyId claim for Secretary (stored in user metadata or assigned faculty)
        // Secretaries typically work for a specific faculty - we'll check if they have a faculty assignment
        if (user.Role == UserRole.Secretary)
        {
            // Check if there's a professor record for secretary (some secretaries might have dual role)
            // Or we can add a dedicated SecretaryFaculty table in the future
            // For now, we'll use a convention: secretary email contains faculty code
            // Better approach: Add FacultyId to ApplicationUser for secretaries

            // Fallback: If no specific faculty, get the first active faculty
            var faculty = await _dbContext.Faculties
                .AsNoTracking()
                .Where(f => f.IsActive)
                .FirstOrDefaultAsync();

            if (faculty != null)
            {
                identity.AddClaim(new Claim("FacultyId", faculty.Id.ToString()));
            }
        }

        return identity;
    }
}
