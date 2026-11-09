// RaceDay South Africa Client Scripts & Accessibility Controller (WCAG 2.1 AA)
document.addEventListener('DOMContentLoaded', function () {
    console.log('RaceDay MVC Loaded with Accessibility Suite');

    // Load saved preferences
    const contrastMode = localStorage.getItem('raceday_contrast') || 'normal';
    const fontSize = localStorage.getItem('raceday_fontsize') || 'normal';
    const focusMode = localStorage.getItem('raceday_focus') || 'normal';

    if (contrastMode === 'high') {
        document.body.setAttribute('data-contrast', 'high');
    }
    if (fontSize === 'md') {
        document.body.classList.add('font-size-md');
    } else if (fontSize === 'lg') {
        document.body.classList.add('font-size-lg');
    }
    if (focusMode === 'high') {
        document.body.classList.add('high-focus-mode');
    }

    // Contrast Toggle
    const contrastBtn = document.getElementById('btn-toggle-contrast');
    if (contrastBtn) {
        contrastBtn.addEventListener('click', function () {
            const isHigh = document.body.getAttribute('data-contrast') === 'high';
            if (isHigh) {
                document.body.removeAttribute('data-contrast');
                localStorage.setItem('raceday_contrast', 'normal');
                announceA11y('Standard contrast mode active');
            } else {
                document.body.setAttribute('data-contrast', 'high');
                localStorage.setItem('raceday_contrast', 'high');
                announceA11y('High contrast mode enabled');
            }
        });
    }

    // Font Resize Handlers
    const fontNormalBtn = document.getElementById('btn-font-normal');
    const fontLargeBtn = document.getElementById('btn-font-large');
    const fontXlBtn = document.getElementById('btn-font-xl');

    if (fontNormalBtn) {
        fontNormalBtn.addEventListener('click', function () {
            document.body.classList.remove('font-size-md', 'font-size-lg');
            localStorage.setItem('raceday_fontsize', 'normal');
            announceA11y('Standard text size set');
        });
    }
    if (fontLargeBtn) {
        fontLargeBtn.addEventListener('click', function () {
            document.body.classList.remove('font-size-lg');
            document.body.classList.add('font-size-md');
            localStorage.setItem('raceday_fontsize', 'md');
            announceA11y('Text size increased to 120%');
        });
    }
    if (fontXlBtn) {
        fontXlBtn.addEventListener('click', function () {
            document.body.classList.remove('font-size-md');
            document.body.classList.add('font-size-lg');
            localStorage.setItem('raceday_fontsize', 'lg');
            announceA11y('Text size increased to 140%');
        });
    }

    // Live Region Announcement Helper
    window.announceA11y = function(message) {
        const liveRegion = document.getElementById('a11y-announcements');
        if (liveRegion) {
            liveRegion.textContent = '';
            setTimeout(() => {
                liveRegion.textContent = message;
            }, 50);
        }
    };
});
