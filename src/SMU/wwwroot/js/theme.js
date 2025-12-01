// Theme Management System for SMU
// Handles dark mode toggle, persistence, and system preference detection

window.themeManager = {
    // Initialize theme on page load
    initializeTheme: function() {
        // Check localStorage first
        let theme = localStorage.getItem('smu-theme');

        // If no saved preference, check system preference
        if (!theme) {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            theme = prefersDark ? 'dark' : 'light';
        }

        // Apply theme
        this.applyTheme(theme);

        // Listen for system preference changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            // Only apply if user hasn't manually set a preference
            if (!localStorage.getItem('smu-theme')) {
                const newTheme = e.matches ? 'dark' : 'light';
                this.applyTheme(newTheme);
            }
        });
    },

    // Get current theme
    getTheme: function() {
        return localStorage.getItem('smu-theme') ||
               (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    },

    // Set theme
    setTheme: function(theme) {
        localStorage.setItem('smu-theme', theme);
        this.applyTheme(theme);
    },

    // Toggle between light and dark
    toggleTheme: function() {
        const currentTheme = this.getTheme();
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
        return newTheme;
    },

    // Apply theme to document
    applyTheme: function(theme) {
        const html = document.documentElement;

        if (theme === 'dark') {
            html.classList.add('dark');
        } else {
            html.classList.remove('dark');
        }

        // Add smooth transition for theme changes (avoid flashing on initial load)
        if (!html.hasAttribute('data-theme-initialized')) {
            html.setAttribute('data-theme-initialized', 'true');
            setTimeout(() => {
                html.style.transition = 'background-color 0.3s ease, color 0.3s ease';
            }, 100);
        }
    }
};

// Initialize theme immediately to prevent flash
(function() {
    const theme = localStorage.getItem('smu-theme') ||
                  (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');

    if (theme === 'dark') {
        document.documentElement.classList.add('dark');
    }
})();
