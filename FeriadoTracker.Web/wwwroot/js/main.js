import { createCountdown } from './countdown.js';
import { setupDestaque } from './destaque.js';
import { setupPushButtons } from './push.js';
import { setupShareButton } from './share.js';

document.addEventListener('DOMContentLoaded', function () {
    setupPushButtons([document.getElementById('btnLembrete')]);

    const section = document.getElementById('destaque');
    if (!section) return;

    const countdown = createCountdown(section);
    const destaque = setupDestaque({
        section,
        proxima: Number(section.dataset.proxima),
        countdown
    });
    countdown.start();

    const shareBtn = document.getElementById('btnShare');
    if (shareBtn) {
        setupShareButton(shareBtn, () => {
            const f = destaque.atual();
            return { nome: f.nome, inicio: f.inicioIso };
        });
    }
});
