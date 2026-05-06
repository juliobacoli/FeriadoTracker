let container = null;

function ensureContainer() {
    if (container) return container;
    container = document.createElement('div');
    container.className = 'toast-container';
    container.setAttribute('role', 'status');
    container.setAttribute('aria-live', 'polite');
    document.body.appendChild(container);
    return container;
}

export function showToast(message, type = 'info', duration = 3500) {
    const root = ensureContainer();

    const el = document.createElement('div');
    el.className = `toast toast-${type}`;
    el.textContent = message;
    root.appendChild(el);

    requestAnimationFrame(() => el.classList.add('is-visible'));

    setTimeout(() => {
        el.classList.remove('is-visible');
        el.addEventListener('transitionend', () => el.remove(), { once: true });
        setTimeout(() => el.remove(), 400);
    }, duration);
}
