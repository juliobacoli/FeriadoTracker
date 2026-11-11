import { showToast } from './toast.js';

function diasAte(inicioIso) {
    const [a, m, d] = inicioIso.split('-').map(Number);
    const agora = new Date();
    const hoje = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate());
    return Math.round((new Date(a, m - 1, d) - hoje) / 86400000);
}

function textoCompartilhado({ nome, inicio }) {
    const dias = diasAte(inicio);
    if (dias <= 0) return `Hoje é ${nome}! Veja no FeriadoTracker.`;
    if (dias === 1) return `Amanhã é ${nome}! Veja no FeriadoTracker.`;
    return `Faltam ${dias} dias para ${nome}! Veja no FeriadoTracker.`;
}

/** @param {() => {nome: string, inicio: string}} feriadoAtual */
export function setupShareButton(btn, feriadoAtual) {
    btn.hidden = false;
    const rotulo = btn.querySelector('span');
    let timer = null;

    btn.addEventListener('click', async () => {
        if (navigator.share) {
            try {
                await navigator.share({
                    title: 'FeriadoTracker',
                    text: textoCompartilhado(feriadoAtual()),
                    url: window.location.origin
                });
            } catch (err) {
                if (err.name !== 'AbortError') console.error('Erro ao compartilhar:', err);
            }
            return;
        }

        try {
            await navigator.clipboard.writeText(window.location.origin);
            rotulo.textContent = 'Link copiado';
            btn.classList.add('is-done');
            showToast('Link copiado. É só colar na conversa.', 'success');
            clearTimeout(timer);
            timer = setTimeout(() => {
                rotulo.textContent = 'Compartilhar';
                btn.classList.remove('is-done');
            }, 3000);
        } catch (err) {
            console.error('Erro ao copiar link:', err);
            showToast('Não foi possível copiar o link.', 'error');
        }
    });
}
