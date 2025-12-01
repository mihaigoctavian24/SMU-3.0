using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for user management
/// </summary>
public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(UserFilter? filter = null);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<ServiceResult> DeactivateAsync(Guid id);
    Task<ServiceResult> ActivateAsync(Guid id);
    Task<ServiceResult> ResetPasswordAsync(Guid id, string newPassword);
    Task<ServiceResult> ChangeRoleAsync(Guid id, UserRole newRole);
    Task<List<RoleStatsDto>> GetRoleStatsAsync();
}
