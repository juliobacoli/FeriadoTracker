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
    const semMovimento = window.matchMedia('(prefers-reduced-motion: reduce)');

    let atual = proxima;

    function mostrar(indice) {
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
    }

    /** Leva o foco ao nome; no celular, rola até o destaque, que fica acima da lista. */
    function focarDestaque() {
        if (empilhado.matches) {
            section.scrollIntoView({ behavior: semMovimento.matches ? 'auto' : 'smooth', block: 'start' });
        }
        el.nome.focus({ preventScroll: true });
    }

    linhas.forEach(l => {
        l.addEventListener('click', () => {
            mostrar(Number(l.dataset.indice));
            focarDestaque();
        });
    });

    el.voltar.querySelector('button').addEventListener('click', () => {
        mostrar(proxima);
        focarDestaque();
    });

    countdown.definir(linha(proxima).dataset, true);

    return { atual: () => linha(atual).dataset };
}
