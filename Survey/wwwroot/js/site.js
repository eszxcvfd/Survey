// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Theme Toggle
function toggleTheme() {
    document.body.classList.toggle('dark-theme');
    localStorage.setItem('theme', document.body.classList.contains('dark-theme') ? 'dark' : 'light');
}

// Load saved theme preference
if (localStorage.getItem('theme') === 'dark') {
    document.body.classList.add('dark-theme');
}

// Navigation Drawer Toggle
function toggleDrawer() {
    const drawer = document.querySelector('.navigation-drawer');
    drawer.classList.toggle('visible');
}

// User Menu Toggle
function toggleUserMenu(event) {
    event.stopPropagation();
    const menu = document.getElementById('userMenu');
    
    if (menu.style.display === 'none' || !menu.style.display) {
        menu.style.display = 'block';
    } else {
        menu.style.display = 'none';
    }
}

// Close user menu when clicking outside
document.addEventListener('click', function(event) {
    const menu = document.getElementById('userMenu');
    const profile = document.querySelector('.user-profile');
    
    if (menu && !menu.contains(event.target) && !profile?.contains(event.target)) {
        menu.style.display = 'none';
    }
});

// Auto-hide toast notifications
document.addEventListener('DOMContentLoaded', function() {
    const toasts = document.querySelectorAll('.toast');
    
    toasts.forEach(function(toast) {
        // Add animation classes
        toast.style.animation = 'slideIn 0.3s ease-out';
        
        // Auto-hide after 4 seconds
        setTimeout(function() {
            toast.style.animation = 'slideOut 0.3s ease-in';
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            
            // Remove from DOM after animation
            setTimeout(function() {
                toast.remove();
            }, 300);
        }, 4000);
        
        // Allow manual close on click
        toast.style.cursor = 'pointer';
        toast.addEventListener('click', function() {
            toast.style.animation = 'slideOut 0.3s ease-in';
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            
            setTimeout(function() {
                toast.remove();
            }, 300);
        });
    });
});
