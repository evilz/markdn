window.themeManager = {
    init: function () {
        const theme = localStorage.getItem("picoPreferredColorScheme") || 
                      (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
        this.setTheme(theme);
    },
    toggle: function () {
        const currentTheme = document.documentElement.getAttribute("data-theme");
        const newTheme = currentTheme === "dark" ? "light" : "dark";
        this.setTheme(newTheme);
        return newTheme;
    },
    setTheme: function (theme) {
        document.documentElement.setAttribute("data-theme", theme);
        if (theme === "dark") {
            document.documentElement.classList.add("dark");
        } else {
            document.documentElement.classList.remove("dark");
        }
        localStorage.setItem("picoPreferredColorScheme", theme);
    }
};

// Initialize on load
window.themeManager.init();
