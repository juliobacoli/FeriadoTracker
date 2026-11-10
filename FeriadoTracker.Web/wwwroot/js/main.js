import { createCountdown } from './countdown.js';
import { setupPushButton } from './push.js';
import { setupShareButton } from './share.js';

document.addEventListener('DOMContentLoaded', function () {
    const pushBtn = document.getElementById('pushToggle');
    if (pushBtn) setupPushButton(pushBtn);

    const destaque = document.getElementById('destaque');
    if (!destaque) return;

    const countdown = createCountdown(destaque);
    countdown.definir(destaque.dataset);
    countdown.start();

    const shareBtn = document.getElementById('btnShare');
    if (shareBtn) {
        setupShareButton(shareBtn, () => ({
            nome: destaque.dataset.nome,
            inicio: destaque.dataset.inicio
        }));
    }
});
