using Microsoft.JSInterop;

namespace SMU.Services;

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "light";

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetThemeAsync()
    {
        try
        {
            var theme = await _jsRuntime.InvokeAsync<string>("themeManager.getTheme");
            _currentTheme = theme ?? "light";
            return _currentTheme;
        }
        catch
        {
            return _currentTheme;
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        try
        {
            _currentTheme = theme;
            await _jsRuntime.InvokeVoidAsync("themeManager.setTheme", theme);
        }
        catch
        {
            // Fallback silently if JS is not ready
        }
    }

    public async Task ToggleThemeAsync()
    {
        try
        {
            var newTheme = await _jsRuntime.InvokeAsync<string>("themeManager.toggleTheme");
            _currentTheme = newTheme ?? "light";
        }
        catch
        {
            // Fallback: toggle locally
            _currentTheme = _currentTheme == "light" ? "dark" : "light";
        }
    }

    public async Task InitializeThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("themeManager.initializeTheme");
            _currentTheme = await GetThemeAsync();
        }
        catch
        {
            // Fallback silently if JS is not ready
        }
    }
}
