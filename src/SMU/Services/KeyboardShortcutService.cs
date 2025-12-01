using Microsoft.JSInterop;

namespace SMU.Services;

/// <summary>
/// Service for managing keyboard shortcuts with JavaScript interop
/// </summary>
public class KeyboardShortcutService : IKeyboardShortcutService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, KeyboardShortcut> _shortcuts = new();
    private DotNetObjectReference<KeyboardShortcutService>? _dotNetReference;
    private IJSObjectReference? _jsModule;
    private bool _isInitialized;

    public event EventHandler<ShortcutTriggeredEventArgs>? ShortcutTriggered;

    public KeyboardShortcutService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize the keyboard shortcut service
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            // Load the JavaScript module
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/keyboard.js");

            // Create .NET reference for callbacks
            _dotNetReference = DotNetObjectReference.Create(this);

            // Initialize JavaScript keyboard manager
            await _jsModule.InvokeVoidAsync("keyboardShortcuts.initialize", _dotNetReference);

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize keyboard shortcuts: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback when a shortcut is triggered
    /// </summary>
    [JSInvokable]
    public async Task OnShortcutTriggered(string keys)
    {
        if (_shortcuts.TryGetValue(keys, out var shortcut))
        {
            // Execute the action
            if (shortcut.Action != null)
            {
                await shortcut.Action();
            }

            // Raise event
            ShortcutTriggered?.Invoke(this, new ShortcutTriggeredEventArgs { Keys = keys });
        }
    }

    /// <summary>
    /// Register a keyboard shortcut
    /// </summary>
    public async Task RegisterShortcutAsync(string keys, string description, string category, Func<Task> action)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        // Store shortcut
        _shortcuts[keys] = new KeyboardShortcut
        {
            Keys = keys,
            Description = description,
            Category = category,
            Action = action
        };

        // Register in JavaScript
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("keyboardShortcuts.register", keys, description, category);
        }
    }

    /// <summary>
    /// Unregister a keyboard shortcut
    /// </summary>
    public async Task UnregisterShortcutAsync(string keys)
    {
        _shortcuts.Remove(keys);

        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("keyboardShortcuts.unregister", keys);
        }
    }

    /// <summary>
    /// Get all registered shortcuts
    /// </summary>
    public Task<List<KeyboardShortcut>> GetAllShortcutsAsync()
    {
        return Task.FromResult(_shortcuts.Values.ToList());
    }

    /// <summary>
    /// Trigger a shortcut programmatically
    /// </summary>
    public async Task TriggerShortcutAsync(string keys)
    {
        await OnShortcutTriggered(keys);
    }

    /// <summary>
    /// Enable or disable shortcuts
    /// </summary>
    public async Task SetEnabledAsync(bool enabled)
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("keyboardShortcuts.setEnabled", enabled);
        }
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("keyboardShortcuts.dispose");
            await _jsModule.DisposeAsync();
        }

        _dotNetReference?.Dispose();
    }
}
