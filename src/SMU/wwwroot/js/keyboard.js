/**
 * Keyboard Shortcut Manager for SMU 3.0
 * Handles global keyboard shortcuts with support for:
 * - Modifier keys (Ctrl, Cmd, Shift, Alt)
 * - Key sequences (G then D)
 * - Context awareness (ignore when typing in inputs)
 * - Cross-platform (Windows/Mac)
 */

class KeyboardShortcutManager {
    constructor() {
        this.shortcuts = new Map();
        this.sequenceBuffer = [];
        this.sequenceTimeout = null;
        this.sequenceTimeoutDuration = 1000; // 1 second to complete sequence
        this.dotNetReference = null;
        this.isEnabled = true;

        // Bind event listener
        this.handleKeyDown = this.handleKeyDown.bind(this);
        this.init();
    }

    /**
     * Initialize keyboard event listeners
     */
    init() {
        document.addEventListener('keydown', this.handleKeyDown, true);
        console.log('Keyboard Shortcut Manager initialized');
    }

    /**
     * Set the .NET reference for callbacks
     */
    setDotNetReference(dotNetRef) {
        this.dotNetReference = dotNetRef;
    }

    /**
     * Enable or disable shortcuts
     */
    setEnabled(enabled) {
        this.isEnabled = enabled;
    }

    /**
     * Check if we should ignore this key event
     * Returns true if user is typing in an input field
     */
    shouldIgnoreEvent(event) {
        const target = event.target;
        const tagName = target.tagName.toLowerCase();
        const isEditable = target.isContentEditable;
        const isInput = tagName === 'input' || tagName === 'textarea' || tagName === 'select';

        return isEditable || isInput;
    }

    /**
     * Normalize key based on platform (Cmd on Mac, Ctrl on Windows)
     */
    normalizeKey(event) {
        let key = event.key;

        // Handle special keys
        if (key === ' ') key = 'Space';
        if (key === 'Escape') key = 'Esc';

        return key;
    }

    /**
     * Build shortcut key string from event
     * Format: "Ctrl+K", "Cmd+Shift+P", "G D" (for sequences)
     */
    buildShortcutKey(event) {
        const parts = [];
        const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;

        // Add modifiers
        if (event.ctrlKey && !isMac) parts.push('Ctrl');
        if (event.metaKey && isMac) parts.push('Cmd');
        if (event.altKey) parts.push('Alt');
        if (event.shiftKey) parts.push('Shift');

        // Add key
        const key = this.normalizeKey(event);
        parts.push(key);

        return parts.join('+');
    }

    /**
     * Handle sequence keys (like G then D)
     */
    handleSequence(key) {
        this.sequenceBuffer.push(key);

        // Clear existing timeout
        if (this.sequenceTimeout) {
            clearTimeout(this.sequenceTimeout);
        }

        // Set new timeout to clear buffer
        this.sequenceTimeout = setTimeout(() => {
            this.sequenceBuffer = [];
        }, this.sequenceTimeoutDuration);

        // Build sequence string
        const sequence = this.sequenceBuffer.join(' ');

        // Check if this sequence matches any registered shortcut
        return sequence;
    }

    /**
     * Main keydown event handler
     */
    handleKeyDown(event) {
        if (!this.isEnabled) return;

        // Ignore if user is typing in an input
        if (this.shouldIgnoreEvent(event)) {
            // Exception: Allow Escape to blur inputs
            if (event.key === 'Escape') {
                event.target.blur();
                this.notifyShortcut('Esc');
            }
            return;
        }

        const key = this.normalizeKey(event);
        const shortcutKey = this.buildShortcutKey(event);

        // Check for single-key shortcuts first (with modifiers)
        if (this.shortcuts.has(shortcutKey)) {
            event.preventDefault();
            this.notifyShortcut(shortcutKey);
            return;
        }

        // Check for sequence shortcuts (no modifiers for sequence keys)
        if (!event.ctrlKey && !event.metaKey && !event.altKey && !event.shiftKey) {
            const sequence = this.handleSequence(key);

            if (this.shortcuts.has(sequence)) {
                event.preventDefault();
                this.sequenceBuffer = []; // Clear buffer on match
                this.notifyShortcut(sequence);
            }
        }
    }

    /**
     * Register a keyboard shortcut
     * @param {string} keys - Shortcut keys (e.g., "Ctrl+K", "G D")
     * @param {string} description - Description of what the shortcut does
     * @param {string} category - Category (Navigation, Actions, General)
     */
    registerShortcut(keys, description, category = 'General') {
        this.shortcuts.set(keys, { description, category });
        console.log(`Registered shortcut: ${keys} - ${description}`);
    }

    /**
     * Unregister a keyboard shortcut
     */
    unregisterShortcut(keys) {
        this.shortcuts.delete(keys);
    }

    /**
     * Get all registered shortcuts
     */
    getAllShortcuts() {
        const shortcuts = [];
        this.shortcuts.forEach((value, key) => {
            shortcuts.push({
                keys: key,
                description: value.description,
                category: value.category
            });
        });
        return shortcuts;
    }

    /**
     * Notify .NET about triggered shortcut
     */
    notifyShortcut(keys) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnShortcutTriggered', keys);
        } else {
            console.warn('No .NET reference set for keyboard shortcuts');
        }
    }

    /**
     * Cleanup
     */
    dispose() {
        document.removeEventListener('keydown', this.handleKeyDown, true);
        if (this.sequenceTimeout) {
            clearTimeout(this.sequenceTimeout);
        }
    }
}

// Global instance
window.keyboardManager = new KeyboardShortcutManager();

// Export functions for Blazor interop
window.keyboardShortcuts = {
    initialize: (dotNetRef) => {
        window.keyboardManager.setDotNetReference(dotNetRef);
    },

    register: (keys, description, category) => {
        window.keyboardManager.registerShortcut(keys, description, category);
    },

    unregister: (keys) => {
        window.keyboardManager.unregisterShortcut(keys);
    },

    getAll: () => {
        return window.keyboardManager.getAllShortcuts();
    },

    setEnabled: (enabled) => {
        window.keyboardManager.setEnabled(enabled);
    },

    dispose: () => {
        window.keyboardManager.dispose();
    }
};
