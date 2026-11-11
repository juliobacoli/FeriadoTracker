const ICONES = {
    success: '<path d="M20 6L9 17l-5-5"/>',
    error: '<circle cx="12" cy="12" r="9"/><path d="M12 7.5v5.5M12 16.5h.01"/>'
};

let container = null;
let atual = null;
let timer = null;

function ensureContainer() {
    if (container) return container;
    container = document.createElement('div');
    container.className = 'toast-container';
    container.setAttribute('role', 'status');
    container.setAttribute('aria-live', 'polite');
    document.body.appendChild(container);
    return container;
}

/** Aviso no topo. Um por vez: um aviso novo substitui o anterior. */
export function showToast(message, type = 'info', duration = 3000) {
    const root = ensureContainer();

    clearTimeout(timer);
    atual?.remove();

    const el = document.createElement('div');
    el.className = `toast toast-${type}`;

    if (ICONES[type]) {
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '2.5');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        svg.setAttribute('aria-hidden', 'true');
        svg.innerHTML = ICONES[type];
        el.appendChild(svg);
    }

    const texto = document.createElement('span');
    texto.textContent = message;
    el.appendChild(texto);

    root.appendChild(el);
    atual = el;
    requestAnimationFrame(() => el.classList.add('is-visible'));

    timer = setTimeout(() => {
        el.classList.remove('is-visible');
        el.addEventListener('transitionend', () => el.remove(), { once: true });
        setTimeout(() => el.remove(), 400);
    }, duration);
}
