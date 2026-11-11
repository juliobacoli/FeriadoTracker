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
    if (!overlay || typeof overlay.showModal !== 'function') {
        button.addEventListener('click', () => {
            showToast('Para ativar notificações no iPhone, adicione o app à tela inicial primeiro.', 'info');
        });
        return;
    }

    // <dialog> nativo: showModal prende o foco e o Esc fecha sozinho.
    document.getElementById('iosInstallClose')?.addEventListener('click', () => overlay.close());
    // O conteúdo ocupa o dialog inteiro; clique com alvo no próprio dialog é clique no fundo.
    overlay.addEventListener('click', e => { if (e.target === overlay) overlay.close(); });

    button.addEventListener('click', () => { if (!overlay.open) overlay.showModal(); });
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

/** Atualiza rótulo, ícone (via aria-pressed) e title de cada botão de lembrete. */
function renderEstado(buttons, active) {
    buttons.forEach(button => {
        button.setAttribute('aria-pressed', active ? 'true' : 'false');
        button.title = active ? 'Desativar lembretes' : 'Ativar lembretes';
        const rotulo = button.querySelector('[data-rotulo-on]');
        if (rotulo) rotulo.textContent = active ? rotulo.dataset.rotuloOn : rotulo.dataset.rotuloOff;
    });
}

/**
 * Liga os botões de lembrete ao push.
 * Todos compartilham o mesmo estado de inscrição.
 */
export async function setupPushButtons(buttons) {
    buttons = buttons.filter(Boolean);
    if (buttons.length === 0) return;

    if (isIosNonStandalone()) {
        buttons.forEach(setupIosInstallModal);
        return;
    }

    if (!isSupported()) {
        buttons.forEach(b => { b.hidden = true; });
        return;
    }

    if (Notification.permission === 'denied') {
        buttons.forEach(b => {
            b.disabled = true;
            b.title = 'Notificações bloqueadas pelo navegador';
            b.setAttribute('aria-pressed', 'false');
        });
        return;
    }

    async function refresh() {
        renderEstado(buttons, !!(await getCurrentSubscription()));
    }

    async function alternar() {
        buttons.forEach(b => { b.disabled = true; });
        try {
            const sub = await getCurrentSubscription();
            if (sub) {
                await unsubscribe();
                showToast('Lembretes desativados.', 'info');
            } else {
                await subscribe();
                showToast('Lembretes ativados. Você recebe um aviso alguns dias antes de cada feriado.', 'success');
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
                    msg = 'Não foi possível alterar os lembretes. Tente novamente.';
            }
            showToast(msg, 'error');
        } finally {
            buttons.forEach(b => { b.disabled = false; });
        }
    }

    buttons.forEach(b => b.addEventListener('click', alternar));

    await refresh();
}
