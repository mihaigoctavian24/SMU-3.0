namespace SMU.Services;

/// <summary>
/// Service for managing keyboard shortcuts in the application
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Event triggered when a keyboard shortcut is activated
    /// </summary>
    event EventHandler<ShortcutTriggeredEventArgs>? ShortcutTriggered;

    /// <summary>
    /// Initialize the keyboard shortcut service and register default shortcuts
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Register a keyboard shortcut
    /// </summary>
    /// <param name="keys">Shortcut keys (e.g., "Ctrl+K", "G D")</param>
    /// <param name="description">Description of the shortcut</param>
    /// <param name="category">Category (Navigation, Actions, General)</param>
    /// <param name="action">Action to execute when shortcut is triggered</param>
    Task RegisterShortcutAsync(string keys, string description, string category, Func<Task> action);

    /// <summary>
    /// Unregister a keyboard shortcut
    /// </summary>
    Task UnregisterShortcutAsync(string keys);

    /// <summary>
    /// Get all registered shortcuts
    /// </summary>
    Task<List<KeyboardShortcut>> GetAllShortcutsAsync();

    /// <summary>
    /// Trigger a shortcut programmatically
    /// </summary>
    Task TriggerShortcutAsync(string keys);

    /// <summary>
    /// Enable or disable shortcuts
    /// </summary>
    Task SetEnabledAsync(bool enabled);
}

/// <summary>
/// Keyboard shortcut definition
/// </summary>
public class KeyboardShortcut
{
    public string Keys { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Func<Task>? Action { get; set; }
}

/// <summary>
/// Event arguments for shortcut triggered event
/// </summary>
public class ShortcutTriggeredEventArgs : EventArgs
{
    public string Keys { get; set; } = string.Empty;
}
