const VAPID_ENDPOINT = '/api/push/vapid-public-key';
const SUBSCRIBE_ENDPOINT = '/api/push/subscribe';
const UNSUBSCRIBE_ENDPOINT = '/api/push/unsubscribe';

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
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            endpoint: json.endpoint,
            p256dh: json.keys.p256dh,
            auth: json.keys.auth
        })
    });

    if (!response.ok) {
        await sub.unsubscribe();
        throw new Error('Falha ao registrar no servidor');
    }
}

async function unsubscribe() {
    const sub = await getCurrentSubscription();
    if (!sub) return;

    await fetch(UNSUBSCRIBE_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ endpoint: sub.endpoint })
    });
    await sub.unsubscribe();
}

export async function setupPushButton(button) {
    if (!button) return;

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
            } else {
                await subscribe();
            }
            await refresh();
        } catch (err) {
            console.error('Push toggle falhou', err);
        } finally {
            button.disabled = false;
        }
    });

    await refresh();
}
