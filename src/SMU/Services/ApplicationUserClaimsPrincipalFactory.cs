using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Custom claims principal factory that adds the Role claim from ApplicationUser.Role
/// This enables the authorization policies to work with RequireClaim("Role", ...)
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
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

        return identity;
    }
}
