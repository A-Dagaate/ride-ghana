// Register service worker for PWA / offline support
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js')
            .catch(err => console.warn('SW registration failed:', err));
    });
}

// Auto-dismiss flash alerts after 5s
document.querySelectorAll('.alert-dismissible').forEach(el => {
    setTimeout(() => {
        const bsAlert = bootstrap.Alert.getOrCreateInstance(el);
        bsAlert?.close();
    }, 5000);
});

// Prevent double-submit on forms
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', () => {
        form.querySelectorAll('[type="submit"]').forEach(btn => {
            btn.disabled = true;
            btn.dataset.original = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Please wait…';
        });
    });
});

// Date validation: ensure end > start
const startInputs = document.querySelectorAll('input[name="start"], input[name="StartDate"]');
const endInputs   = document.querySelectorAll('input[name="end"],   input[name="EndDate"]');

startInputs.forEach(s => {
    s.addEventListener('change', () => {
        endInputs.forEach(e => { if (e.value && e.value <= s.value) e.value = ''; });
    });
});
