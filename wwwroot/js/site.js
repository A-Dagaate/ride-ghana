// ── PWA Service Worker ────────────────────────────────────────────────────────
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js')
            .catch(err => console.warn('SW registration failed:', err));
    });
}

// ── Currency Toggle ───────────────────────────────────────────────────────────
const RG = (() => {
    const STORAGE_KEY = 'rg_currency';
    const GHS_TO_USD  = window.RG_GHS_TO_USD || 0.065;

    let current = localStorage.getItem(STORAGE_KEY) || 'GHS';

    function format(ghs) {
        if (current === 'USD') {
            const usd = ghs * GHS_TO_USD;
            return 'USD\u00A0' + usd.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
        return 'GHS\u00A0' + ghs.toLocaleString('en-GH', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
    }

    function applyPrices() {
        // Update all tagged price elements
        document.querySelectorAll('[data-ghs]').forEach(el => {
            el.textContent = format(parseFloat(el.dataset.ghs));
        });

        // Keep hidden currency inputs in sync (booking form)
        document.querySelectorAll('input[name="SelectedCurrency"]').forEach(el => {
            el.value = current;
        });

        // Highlight active option in the toggle button
        const ghsEl = document.getElementById('optGhs');
        const usdEl = document.getElementById('optUsd');
        if (ghsEl && usdEl) {
            ghsEl.classList.toggle('rg-active', current === 'GHS');
            usdEl.classList.toggle('rg-active', current === 'USD');
        }

        // Let individual pages react (e.g. Book page live cost calculator)
        document.dispatchEvent(new CustomEvent('rg:currencyChanged', { detail: { currency: current, format } }));
    }

    function toggleCurrency() {
        current = current === 'GHS' ? 'USD' : 'GHS';
        localStorage.setItem(STORAGE_KEY, current);
        applyPrices();
    }

    function getCurrent() { return current; }
    function getFormat()  { return format; }

    document.addEventListener('DOMContentLoaded', applyPrices);

    return { toggleCurrency, getCurrent, getFormat };
})();

// ── Auto-dismiss flash alerts after 5s ───────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.alert-dismissible').forEach(el => {
        setTimeout(() => bootstrap.Alert.getOrCreateInstance(el)?.close(), 5000);
    });
});

// ── Prevent double-submit ─────────────────────────────────────────────────────
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', () => {
        form.querySelectorAll('[type="submit"]').forEach(btn => {
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Please wait…';
        });
    });
});

// ── Date validation: end must be after start ──────────────────────────────────
document.querySelectorAll('input[name="start"], input[name="StartDate"]').forEach(s => {
    s.addEventListener('change', () => {
        document.querySelectorAll('input[name="end"], input[name="EndDate"]').forEach(e => {
            if (e.value && e.value <= s.value) e.value = '';
        });
    });
});
