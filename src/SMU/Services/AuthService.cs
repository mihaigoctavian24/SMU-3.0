using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using System.Security.Claims;

namespace SMU.Services;

/// <summary>
/// Authentication service for managing user login/logout
/// </summary>
public class AuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger<AuthService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", email);
            return AuthResult.Failed("Email sau parolă incorectă.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            return AuthResult.Failed("Contul este dezactivat. Contactați administratorul.");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return AuthResult.Success(user);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserId} is locked out", user.Id);
            return AuthResult.Failed("Contul este blocat temporar. Încercați mai târziu.");
        }

        if (result.RequiresTwoFactor)
        {
            return AuthResult.RequiresTwoFactor();
        }

        _logger.LogWarning("Failed login attempt for user {UserId}", user.Id);
        return AuthResult.Failed("Email sau parolă incorectă.");
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return await _userManager.GetUserAsync(principal);
    }

    /// <summary>
    /// Gets the current user with Professor navigation property loaded
    /// </summary>
    public async Task<ApplicationUser?> GetCurrentUserWithProfessorAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _dbContext.Users
            .Include(u => u.Professor)
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
    }

    /// <summary>
    /// Gets the current user with Student navigation property loaded
    /// </summary>
    public async Task<ApplicationUser?> GetCurrentUserWithStudentAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _dbContext.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, UserRole role)
    {
        return await _userManager.IsInRoleAsync(user, role.ToString());
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(ClaimsPrincipal principal, string currentPassword, string newPassword)
    {
        var user = await GetCurrentUserAsync(principal);

        if (user == null)
        {
            _logger.LogWarning("Change password attempted for unauthenticated user");
            return PasswordChangeResult.Failed("Utilizatorul nu este autentificat.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} changed password successfully", user.Id);
            return PasswordChangeResult.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Failed password change for user {UserId}: {Errors}", user.Id, errors);

        return PasswordChangeResult.Failed(
            result.Errors.Any(e => e.Code.Contains("Password"))
                ? "Parola curentă este incorectă."
                : errors
        );
    }

    public async Task<UserRole?> GetUserRoleAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            _logger.LogWarning("GetUserRole attempted for non-existent user: {UserId}", userId);
            return null;
        }

        return user.Role;
    }

    public async Task<UserRole?> GetCurrentUserRoleAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        return user?.Role;
    }
}

public class AuthResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ApplicationUser? User { get; private set; }
    public bool RequiresTwoFactorAuth { get; private set; }

    public static AuthResult Success(ApplicationUser user) => new()
    {
        Succeeded = true,
        User = user
    };

    public static AuthResult Failed(string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error
    };

    public static AuthResult RequiresTwoFactor() => new()
    {
        Succeeded = false,
        RequiresTwoFactorAuth = true
    };
}

public class PasswordChangeResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static PasswordChangeResult Success() => new()
    {
        Succeeded = true
    };

    public static PasswordChangeResult Failed(string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error
    };
}
