using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// User management service implementation
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync(UserFilter? filter = null)
    {
        var query = _context.Users
            .Include(u => u.Student)
            .Include(u => u.Professor)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.Role.HasValue)
            {
                query = query.Where(u => u.Role == filter.Role.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.Email!.ToLower().Contains(searchLower));
            }
        }

        var users = await query
            .OrderBy(u => u.Role)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                StudentId = u.Student != null ? u.Student.Id : null,
                ProfessorId = u.Professor != null ? u.Professor.Id : null
            })
            .ToListAsync();

        return users;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Professor)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            StudentId = user.Student?.Id,
            ProfessorId = user.Professor?.Id
        };
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateUserDto dto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResult<Guid>.Failed("Email-ul este deja folosit.");
            }

            // Verify faculty exists if creating professor
            if (dto.Role == UserRole.Professor && dto.FacultyId.HasValue)
            {
                var faculty = await _context.Faculties.FindAsync(dto.FacultyId);
                if (faculty == null)
                {
                    return ServiceResult<Guid>.Failed("Facultatea selectată nu există.");
                }
            }

            // Create user account
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create user: {Errors}", errors);
                return ServiceResult<Guid>.Failed($"Eroare la crearea contului: {errors}");
            }

            // Create professor record if role is Professor
            if (dto.Role == UserRole.Professor && dto.FacultyId.HasValue)
            {
                var professor = new Professor
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FacultyId = dto.FacultyId.Value
                };

                _context.Professors.Add(professor);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("User created successfully: {UserId} - {Email} - Role: {Role}",
                user.Id, dto.Email, dto.Role);

            return ServiceResult<Guid>.Success(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ServiceResult<Guid>.Failed("Eroare la crearea utilizatorului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return ServiceResult.Failed("Utilizatorul nu a fost găsit.");
            }

            // Update user info
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.IsActive = dto.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failed($"Eroare la actualizare: {errors}");
            }

            _logger.LogInformation("User updated successfully: {UserId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ServiceResult.Failed("Eroare la actualizarea utilizatorului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> DeactivateAsync(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return ServiceResult.Failed("Utilizatorul nu a fost găsit.");
            }

            // Prevent deactivating the last admin
            if (user.Role == UserRole.Admin)
            {
                var activeAdminCount = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Admin && u.IsActive);

                if (activeAdminCount <= 1)
                {
                    return ServiceResult.Failed("Nu puteți dezactiva ultimul administrator activ.");
                }
            }

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.Failed("Eroare la dezactivarea utilizatorului.");
            }

            _logger.LogInformation("User deactivated: {UserId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return ServiceResult.Failed("Eroare la dezactivarea utilizatorului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> ActivateAsync(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return ServiceResult.Failed("Utilizatorul nu a fost găsit.");
            }

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.Failed("Eroare la activarea utilizatorului.");
            }

            _logger.LogInformation("User activated: {UserId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return ServiceResult.Failed("Eroare la activarea utilizatorului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> ResetPasswordAsync(Guid id, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return ServiceResult.Failed("Utilizatorul nu a fost găsit.");
            }

            // Generate password reset token and reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failed($"Eroare la resetarea parolei: {errors}");
            }

            _logger.LogInformation("Password reset for user: {UserId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return ServiceResult.Failed("Eroare la resetarea parolei. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> ChangeRoleAsync(Guid id, UserRole newRole)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return ServiceResult.Failed("Utilizatorul nu a fost găsit.");
            }

            // Prevent changing role if it's the last admin
            if (user.Role == UserRole.Admin && newRole != UserRole.Admin)
            {
                var activeAdminCount = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Admin && u.IsActive);

                if (activeAdminCount <= 1)
                {
                    return ServiceResult.Failed("Nu puteți schimba rolul ultimului administrator activ.");
                }
            }

            user.Role = newRole;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failed($"Eroare la schimbarea rolului: {errors}");
            }

            _logger.LogInformation("User role changed: {UserId} - New Role: {Role}", id, newRole);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing role for user {UserId}", id);
            return ServiceResult.Failed("Eroare la schimbarea rolului. Vă rugăm încercați din nou.");
        }
    }

    public async Task<List<RoleStatsDto>> GetRoleStatsAsync()
    {
        var roleStats = new List<RoleStatsDto>();

        var roles = Enum.GetValues<UserRole>();

        foreach (var role in roles)
        {
            var count = await _context.Users.CountAsync(u => u.Role == role);

            roleStats.Add(new RoleStatsDto
            {
                Role = role,
                RoleName = GetRoleName(role),
                Description = GetRoleDescription(role),
                UserCount = count
            });
        }

        return roleStats;
    }

    private static string GetRoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Administrator",
        UserRole.Rector => "Rector",
        UserRole.Dean => "Decan",
        UserRole.Secretary => "Secretar",
        UserRole.Professor => "Profesor",
        UserRole.Student => "Student",
        _ => role.ToString()
    };

    private static string GetRoleDescription(UserRole role) => role switch
    {
        UserRole.Admin => "Acces complet la sistem, gestionare utilizatori și configurări",
        UserRole.Rector => "Statistici la nivel de universitate, rapoarte generale",
        UserRole.Dean => "Management la nivel de facultate, aprobare note",
        UserRole.Secretary => "Procesare cereri și administrare studenți",
        UserRole.Professor => "Management note și prezențe pentru cursuri",
        UserRole.Student => "Vizualizare date proprii și înregistrare cereri",
        _ => "Fără descriere"
    };
}
