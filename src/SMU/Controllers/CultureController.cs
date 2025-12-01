using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SMU.Controllers;

/// <summary>
/// Controller for handling culture/language changes.
/// Sets culture cookie and redirects back to the current page.
/// </summary>
[Route("api/culture")]
public class CultureController : Controller
{
    /// <summary>
    /// Sets the culture cookie and redirects to the specified page.
    /// </summary>
    /// <param name="culture">Culture code (e.g., "ro-RO", "en-US")</param>
    /// <param name="redirectUri">URI to redirect after setting culture</param>
    [HttpGet("set")]
    public IActionResult SetCulture(string culture, string redirectUri = "/")
    {
        if (!string.IsNullOrEmpty(culture))
        {
            // Set culture cookie
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false // Allow JavaScript access for client-side preference
                }
            );
        }

        // Redirect back to the page the user was on
        return LocalRedirect(redirectUri);
    }
}
