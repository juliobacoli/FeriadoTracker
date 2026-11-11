import { rolarAte } from './rolagem.js';

/**
 * Lista clicável: cada linha leva o feriado ao destaque e
 * "Voltar para a próxima folga" desfaz a seleção.
 *
 * O servidor renderiza tudo: os textos vêm nos data-* da linha
 * e a faixa de dias num <template id="faixa-N">. Aqui só se copia.
 */
export function setupDestaque({ section, proxima, countdown }) {
    const el = {
        nome: section.querySelector('#focusName'),
        data: section.querySelector('#focusDate'),
        faixa: section.querySelector('#focusStrip'),
        impacto: section.querySelector('#focusImpact'),
        legendaPonte: section.querySelector('#legendPonte'),
        voltar: section.querySelector('#focusBack')
    };
    const linhas = [...document.querySelectorAll('.row[data-indice]')];
    const linha = indice => linhas.find(l => Number(l.dataset.indice) === indice);
    const empilhado = window.matchMedia('(max-width: 959px)');

    let atual = proxima;

    /** Passa a próxima folga para a linha seguinte que vira folga e ainda não acabou. */
    function avancar() {
        const d = new Date();
        const hoje = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        const nova = linhas.find(l => Number(l.dataset.indice) > proxima
            && l.dataset.tipo !== 'FimDeSemana'
            && l.dataset.folgaFimIso >= hoje);
        if (!nova) return false;

        proxima = Number(nova.dataset.indice);
        mostrar(proxima, true);
        return true;
    }

    function mostrar(indice, animar = false) {
        atual = indice;
        const f = linha(indice).dataset;

        el.nome.textContent = f.nome;
        el.data.textContent = f.dataTexto;
        el.faixa.replaceChildren(document.getElementById(`faixa-${indice}`).content.cloneNode(true));
        el.impacto.textContent = f.impacto;
        el.impacto.classList.toggle('is-long', f.tipo === 'Prolongado');
        el.legendaPonte.hidden = f.temPonte !== 'true';
        el.voltar.hidden = indice === proxima;

        linhas.forEach(l => l.setAttribute('aria-pressed', l === linha(indice) ? 'true' : 'false'));

        countdown.definir(f, indice === proxima);

        // Animação de entrada só para mouse/toque: no teclado a troca é repetida e a animação atrasa.
        section.classList.remove('is-troca');
        if (animar) {
            // O reflow entre remover e adicionar a classe dispara a animação de novo.
            void section.offsetWidth;
            section.classList.add('is-troca');
        }
    }

    /** Leva o foco ao nome; no celular, rola até o destaque, que fica acima da lista. */
    function focarDestaque() {
        if (empilhado.matches) {
            rolarAte(section);
        }
        el.nome.focus({ preventScroll: true });
    }

    linhas.forEach(l => {
        // detail é 0 quando o clique vem do teclado (Enter/Espaço).
        l.addEventListener('click', e => {
            mostrar(Number(l.dataset.indice), e.detail > 0);
            focarDestaque();
        });
    });

    el.voltar.querySelector('button').addEventListener('click', e => {
        mostrar(proxima, e.detail > 0);
        focarDestaque();
    });

    // Desktop: passar o mouse numa linha mostra a contagem viva daquele feriado, sem trocar o destaque.
    if (window.matchMedia('(hover: hover)').matches) {
        const vivo = document.createElement('span');
        vivo.className = 'row-live';
        vivo.setAttribute('aria-hidden', 'true');
        const p2 = n => String(n).padStart(2, '0');
        let timer = null;

        function pintar(l) {
            const [a, m, d] = l.dataset.inicioIso.split('-').map(Number);
            const s = Math.floor((new Date(a, m - 1, d) - Date.now()) / 1000);
            vivo.textContent = s > 0
                ? `${Math.floor(s / 86400)}d ${p2(Math.floor(s % 86400 / 3600))}h ${p2(Math.floor(s % 3600 / 60))}min ${p2(s % 60)}s`
                : '';
        }

        linhas.forEach(l => {
            l.addEventListener('mouseenter', () => {
                clearInterval(timer);
                pintar(l);
                l.appendChild(vivo);
                timer = setInterval(() => pintar(l), 1000);
            });
            l.addEventListener('mouseleave', () => {
                clearInterval(timer);
                vivo.remove();
            });
        });
    }

    countdown.aoPassar = avancar;
    countdown.definir(linha(proxima).dataset, true);

    return { atual: () => linha(atual).dataset };
}
