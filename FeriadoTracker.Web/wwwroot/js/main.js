import { setupAvaliacao } from './avaliacao.js';
import { createCountdown } from './countdown.js';
import { setupDestaque } from './destaque.js';
import { setupPushButtons } from './push.js';
import { iniciarRolagem } from './rolagem.js';
import { setupShareButton } from './share.js';

document.addEventListener('DOMContentLoaded', function () {
    iniciarRolagem();
    setupPushButtons([document.getElementById('btnLembrete')]);
    setupAvaliacao();

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
