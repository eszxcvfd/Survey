// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Toggle navigation drawer
function toggleDrawer() {
    const drawer = document.querySelector('.navigation-drawer');
    
    if (window.innerWidth <= 768) {
        drawer.classList.toggle('visible');
    }
}

// Toggle theme (dark/light mode)
function toggleTheme() {
    document.body.classList.toggle('dark-theme');
    
    const isDark = document.body.classList.contains('dark-theme');
    const icon = event.target.closest('md-icon-button').querySelector('.material-symbols-outlined');
    icon.textContent = isDark ? 'light_mode' : 'dark_mode';
}

// Close drawer when clicking outside on mobile
document.addEventListener('click', (e) => {
    if (window.innerWidth <= 768) {
        const drawer = document.querySelector('.navigation-drawer');
        const isClickInsideDrawer = drawer?.contains(e.target);
        const isMenuButton = e.target.closest('md-icon-button[onclick="toggleDrawer()"]');
        
        if (!isClickInsideDrawer && !isMenuButton && drawer?.classList.contains('visible')) {
            drawer.classList.remove('visible');
        }
    }
});

// Toggle user menu
function toggleUserMenu(event) {
    event.stopPropagation();
    const userMenu = document.getElementById('userMenu');
    if (userMenu) {
        const isVisible = userMenu.style.display === 'block';
        userMenu.style.display = isVisible ? 'none' : 'block';
    }
}

// Close user menu when clicking outside
document.addEventListener('click', function(event) {
    const userMenu = document.getElementById('userMenu');
    const userProfile = document.querySelector('.user-profile');
    
    if (userMenu && userProfile) {
        if (!userProfile.contains(event.target) && !userMenu.contains(event.target)) {
            userMenu.style.display = 'none';
        }
    }
});

// Prevent menu from closing when clicking inside it
document.addEventListener('DOMContentLoaded', function() {
    const userMenu = document.getElementById('userMenu');
    if (userMenu) {
        userMenu.addEventListener('click', function(event) {
            // Chỉ close nếu click vào link hoặc button submit
            if (event.target.closest('a') || event.target.closest('button[type="submit"]')) {
                // Let the link/form handle navigation
            } else {
                event.stopPropagation();
            }
        });
    }
});
