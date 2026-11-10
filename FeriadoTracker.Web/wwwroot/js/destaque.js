function diaDaFaixa(dia) {
    const li = document.createElement('li');
    if (dia.classe) li.className = dia.classe;
    li.dataset.iso = dia.iso;
    li.setAttribute('aria-label', dia.rotulo);

    const wd = document.createElement('span');
    wd.className = 'wd';
    wd.setAttribute('aria-hidden', 'true');
    wd.textContent = dia.diaSemana;

    const dn = document.createElement('span');
    dn.className = 'dn';
    dn.setAttribute('aria-hidden', 'true');
    dn.textContent = dia.dia;

    li.append(wd, dn);
    return li;
}

/**
 * Lista clicável: cada linha leva o feriado ao destaque e
 * "Voltar para a próxima folga" desfaz a seleção.
 */
export function setupDestaque({ section, feriados, proxima, countdown }) {
    const el = {
        nome: section.querySelector('#focusName'),
        data: section.querySelector('#focusDate'),
        faixa: section.querySelector('#focusStrip'),
        impacto: section.querySelector('#focusImpact'),
        legendaPonte: section.querySelector('#legendPonte'),
        voltar: section.querySelector('#focusBack')
    };
    const linhas = document.querySelectorAll('.row[data-indice]');
    const empilhado = window.matchMedia('(max-width: 959px)');
    const semMovimento = window.matchMedia('(prefers-reduced-motion: reduce)');

    let atual = proxima;

    function mostrar(indice) {
        atual = indice;
        const f = feriados[indice];

        el.nome.textContent = f.nome;
        el.data.textContent = f.dataTexto;
        el.faixa.replaceChildren(...f.faixa.map(diaDaFaixa));
        el.impacto.textContent = f.impacto;
        el.impacto.classList.toggle('is-long', f.tipo === 'Prolongado');
        el.legendaPonte.hidden = !f.temPonte;
        el.voltar.hidden = indice === proxima;

        linhas.forEach(l => l.setAttribute('aria-pressed', Number(l.dataset.indice) === indice ? 'true' : 'false'));

        countdown.definir(f, indice === proxima);
    }

    /** Leva o foco ao nome; no celular, rola até o destaque, que fica acima da lista. */
    function focarDestaque() {
        if (empilhado.matches) {
            section.scrollIntoView({ behavior: semMovimento.matches ? 'auto' : 'smooth', block: 'start' });
        }
        el.nome.focus({ preventScroll: true });
    }

    linhas.forEach(linha => {
        linha.addEventListener('click', () => {
            mostrar(Number(linha.dataset.indice));
            focarDestaque();
        });
    });

    el.voltar.querySelector('button').addEventListener('click', () => {
        mostrar(proxima);
        focarDestaque();
    });

    countdown.definir(feriados[proxima], true);

    return { atual: () => feriados[atual] };
}
