namespace SMU.Services;

public interface IThemeService
{
    /// <summary>
    /// Gets the current theme (light or dark)
    /// </summary>
    Task<string> GetThemeAsync();

    /// <summary>
    /// Sets the theme and persists it to localStorage
    /// </summary>
    Task SetThemeAsync(string theme);

    /// <summary>
    /// Toggles between light and dark themes
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// Initializes theme from localStorage or system preference
    /// </summary>
    Task InitializeThemeAsync();
}
