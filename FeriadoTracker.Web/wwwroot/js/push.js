import { showToast } from './toast.js';

const VAPID_ENDPOINT = '/api/push/vapid-public-key';
const SUBSCRIBE_ENDPOINT = '/api/push/subscribe';
const UNSUBSCRIBE_ENDPOINT = '/api/push/unsubscribe';

function getAntiforgeryToken() {
    return document.querySelector('meta[name="request-verification-token"]')?.content ?? '';
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const output = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; i++) {
        output[i] = rawData.charCodeAt(i);
    }
    return output;
}

function isSupported() {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
}

function isIosNonStandalone() {
    const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent) || window.navigator.standalone === false;
    return isIos && !window.navigator.standalone;
}

function setupIosInstallModal(button) {
    const overlay = document.getElementById('iosInstallOverlay');
    if (!overlay) {
        button.addEventListener('click', () => {
            showToast('Para ativar notificações no iPhone, adicione o app à tela inicial primeiro.', 'info');
        });
        return;
    }

    const close = () => { overlay.hidden = true; };

    document.getElementById('iosInstallClose')?.addEventListener('click', close);
    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') close(); }, { once: false });

    button.addEventListener('click', () => { overlay.hidden = false; });
}

async function getRegistration() {
    let reg = await navigator.serviceWorker.getRegistration('/');
    if (!reg) {
        reg = await navigator.serviceWorker.register('/sw.js');
    }
    await navigator.serviceWorker.ready;
    return reg;
}

async function getCurrentSubscription() {
    if (!isSupported()) return null;
    const reg = await navigator.serviceWorker.getRegistration('/');
    if (!reg) return null;
    return await reg.pushManager.getSubscription();
}

async function subscribe() {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        throw new Error('Permissão negada');
    }

    const reg = await getRegistration();

    const res = await fetch(VAPID_ENDPOINT);
    if (!res.ok) throw new Error('VAPID indisponível');
    const { publicKey } = await res.json();

    const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey)
    });

    const json = sub.toJSON();
    const response = await fetch(SUBSCRIBE_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiforgeryToken() },
        body: JSON.stringify({
            endpoint: json.endpoint,
            p256dh: json.keys.p256dh,
            auth: json.keys.auth
        })
    });

    if (response.status === 429) {
        await sub.unsubscribe();
        throw new Error('rate-limited');
    }

    if (!response.ok) {
        await sub.unsubscribe();
        throw new Error('Falha ao registrar no servidor');
    }
}

async function unsubscribe() {
    const sub = await getCurrentSubscription();
    if (!sub) return;

    const response = await fetch(UNSUBSCRIBE_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiforgeryToken() },
        body: JSON.stringify({ endpoint: sub.endpoint })
    });

    if (response.status === 429) {
        throw new Error('rate-limited');
    }

    await sub.unsubscribe();
}

const HINT_DISMISSED_KEY = 'push-hint-dismissed';

function setupHint(button) {
    const hint = document.getElementById('pushHint');
    if (!hint) return null;

    if (localStorage.getItem(HINT_DISMISSED_KEY) === '1') return null;

    function hide(persist) {
        hint.hidden = true;
        if (persist) localStorage.setItem(HINT_DISMISSED_KEY, '1');
    }

    const closeBtn = document.getElementById('pushHintClose');
    if (closeBtn) closeBtn.addEventListener('click', () => hide(true));

    button.addEventListener('click', () => hide(true), { once: true });

    return { show: () => { hint.hidden = false; } };
}

export async function setupPushButton(button) {
    if (!button) return;

    if (isIosNonStandalone()) {
        setupIosInstallModal(button);
        return;
    }

    if (!isSupported()) {
        button.hidden = true;
        return;
    }

    if (Notification.permission === 'denied') {
        button.disabled = true;
        button.title = 'Notificações bloqueadas pelo navegador';
        button.setAttribute('aria-pressed', 'false');
        return;
    }

    async function refresh() {
        const sub = await getCurrentSubscription();
        const active = !!sub;
        button.classList.toggle('is-active', active);
        button.title = active ? 'Desativar notificações' : 'Ativar notificações';
        button.setAttribute('aria-pressed', active ? 'true' : 'false');
    }

    button.addEventListener('click', async () => {
        button.disabled = true;
        try {
            const sub = await getCurrentSubscription();
            if (sub) {
                await unsubscribe();
                showToast('Notificações desativadas.', 'info');
            } else {
                await subscribe();
                showToast('Notificações ativadas! Avisaremos antes do próximo feriado.', 'success');
            }
            await refresh();
        } catch (err) {
            console.error('Push toggle falhou', err);
            let msg;
            switch (err && err.message) {
                case 'Permissão negada':
                    msg = 'Permissão negada pelo navegador.';
                    break;
                case 'rate-limited':
                    msg = 'Muitas tentativas. Aguarde 1 minuto e tente novamente.';
                    break;
                default:
                    msg = 'Não foi possível alterar a inscrição. Tente novamente.';
            }
            showToast(msg, 'error');
        } finally {
            button.disabled = false;
        }
    });

    await refresh();

    const current = await getCurrentSubscription();
    if (!current) {
        const hint = setupHint(button);
        if (hint) setTimeout(() => hint.show(), 2000);
    }
}
