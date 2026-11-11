import { showToast } from './toast.js';

const ENDPOINT = '/api/avaliacoes';
const CHAVE = 'avaliado';
const LIMITE = 500;

function jaAvaliou() {
    try { return localStorage.getItem(CHAVE) === '1'; } catch { return false; }
}

function marcarAvaliado() {
    try { localStorage.setItem(CHAVE, '1'); } catch { }
}

function token() {
    return document.querySelector('meta[name="request-verification-token"]')?.content ?? '';
}

/**
 * Avaliação da tela: 5 estrelas e, depois de escolher, comentário opcional e envio.
 * Quem já avaliou não vê o bloco de novo (flag simples no navegador, sem identificador).
 */
export function setupAvaliacao() {
    const card = document.getElementById('avaliacao');
    if (!card || jaAvaliou()) return;

    const estrelas = [...card.querySelectorAll('.estrela-btn')];
    const form = card.querySelector('#avalForm');
    const corpo = card.querySelector('#avalCorpo');
    const comentario = card.querySelector('#avalComentario');
    const contador = card.querySelector('#avalContador');
    const notaTexto = card.querySelector('#avalNota');
    const enviar = card.querySelector('#avalEnviar');
    const pronto = card.querySelector('#avalPronto');
    const comMouse = window.matchMedia('(hover: hover)').matches;

    let nota = 0;
    let enviando = false;

    function pintar(valor, classe) {
        estrelas.forEach(b => b.classList.toggle(classe, Number(b.dataset.valor) <= valor));
    }

    estrelas.forEach(botao => {
        const valor = Number(botao.dataset.valor);

        // Prévia ao passar o mouse. No toque o mouseleave não vem, e a prévia ficaria presa.
        if (comMouse) {
            botao.addEventListener('mouseenter', () => pintar(valor, 'is-hover'));
            botao.addEventListener('mouseleave', () => pintar(0, 'is-hover'));
        }

        botao.addEventListener('click', () => {
            nota = valor;
            pintar(nota, 'is-active');
            estrelas.forEach(b => b.setAttribute('aria-pressed', Number(b.dataset.valor) === nota ? 'true' : 'false'));
            notaTexto.textContent = `${nota} de 5`;
            corpo.hidden = false;
        });
    });

    comentario.addEventListener('input', () => {
        contador.textContent = `${comentario.value.length} / ${LIMITE}`;
    });

    enviar.addEventListener('click', async () => {
        if (nota < 1 || enviando) return;

        enviando = true;
        enviar.disabled = true;
        try {
            const resposta = await fetch(ENDPOINT, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify({ estrelas: nota, comentario: comentario.value })
            });

            if (resposta.status === 429) {
                showToast('Muitas tentativas. Aguarde 1 minuto e tente novamente.', 'error');
                return;
            }
            if (!resposta.ok) throw new Error(`HTTP ${resposta.status}`);

            marcarAvaliado();
            form.hidden = true;
            pronto.hidden = false;
            // O formulário sumiu: leva o foco para o agradecimento, para não deixar quem usa teclado ou leitor de tela solto.
            pronto.focus({ preventScroll: true });
            showToast('Avaliação enviada. Muito obrigado pelo retorno!', 'success');
        } catch (erro) {
            console.error('Falha ao enviar avaliação', erro);
            showToast('Não foi possível enviar a avaliação. Tente novamente.', 'error');
        } finally {
            enviando = false;
            enviar.disabled = false;
        }
    });

    card.hidden = false;
}
