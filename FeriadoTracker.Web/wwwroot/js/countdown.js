const HORA = 3600000;

function pad(n) {
    return String(n).padStart(2, '0');
}

/** 'AAAA-MM-DD' → meia-noite local. */
function parseData(iso) {
    const [a, m, d] = iso.split('-').map(Number);
    return new Date(a, m - 1, d);
}

function addDias(data, n) {
    return new Date(data.getFullYear(), data.getMonth(), data.getDate() + n);
}

function iso(data) {
    return `${data.getFullYear()}-${pad(data.getMonth() + 1)}-${pad(data.getDate())}`;
}

function parte(numero, unidade) {
    const el = document.createElement('span');
    el.className = 'count-part';
    const num = document.createElement('span');
    num.className = 'count-num';
    num.textContent = numero;
    el.appendChild(num);
    if (unidade) {
        const un = document.createElement('span');
        un.className = 'count-unit';
        un.textContent = unidade;
        el.appendChild(un);
    }
    return el;
}

function etiqueta(texto) {
    const el = document.createElement('span');
    el.className = 'count-word';
    el.textContent = texto;
    return el;
}

/**
 * Contagem que cresce conforme o feriado chega:
 * dias (> 48 h) → h e min (até 1 h) → mm:ss (última hora) → modo âmbar no dia.
 * Com mais de 48 h, o número grande são os dias completos e a linha de baixo é o relógio
 * (h, min, s) do que sobra: as duas linhas formam uma conta só.
 */
export function createCountdown(section) {
    const el = {
        kicker: section.querySelector('#focusKicker'),
        count: section.querySelector('#focusCount'),
        exact: section.querySelector('#focusExact'),
        note: section.querySelector('#focusNote')
    };
    const tituloOriginal = document.title;

    // Relógio da linha de baixo: montado uma vez; a cada segundo só os números mudam.
    const relogio = { raiz: document.createDocumentFragment(), numeros: [] };
    {
        const raiz = document.createElement('span');
        raiz.className = 'clock';
        for (const unidade of ['h', 'min', 's']) {
            const item = parte('00', unidade);
            item.className = 'clock-part';
            item.firstChild.className = 'clock-num';
            item.lastChild.className = 'clock-unit';
            relogio.numeros.push(item.firstChild);
            raiz.appendChild(item);
        }
        relogio.raiz = raiz;
    }

    let alvo = null;
    let intervalo = null;
    let ultimaChave = '';

    /**
     * @param feriado Item do JSON de feriados (FeriadoVisao).
     * @param proxima Se é a próxima folga ou um feriado escolhido na lista.
     */
    function definir(feriado, proxima = true) {
        alvo = {
            nome: feriado.nome,
            proxima,
            inicio: parseData(feriado.inicioIso),
            fimFeriado: addDias(parseData(feriado.fimIso), 1),
            fimFolga: addDias(parseData(feriado.folgaFimIso), 1),
            folgaFimIso: feriado.folgaFimIso,
            fimDeSemana: feriado.tipo === 'FimDeSemana',
            nota: feriado.notaNoDia
        };
        ultimaChave = '';
        render();
    }

    function estado(agora) {
        const ms = alvo.inicio - agora;
        const totS = Math.max(0, Math.floor(ms / 1000));
        const h = Math.floor((totS % 86400) / 3600);
        const mi = Math.floor((totS % 3600) / 60);
        const s = totS % 60;

        if (ms > 48 * HORA) {
            const dias = Math.floor(totS / 86400);
            return {
                chave: `d${dias}`,
                partes: [[String(dias), 'dias']],
                relogio: [[pad(h), 'h'], [pad(mi), 'min'], [pad(s), 's']],
                titulo: `${dias} dias para ${alvo.nome}`
            };
        }

        if (ms > HORA) {
            const th = Math.floor(totS / 3600);
            return {
                chave: `h${th}:${mi}`,
                partes: [[String(th), 'h'], [pad(mi), 'min']],
                exact: `Começa à meia-noite. Faltam ${th}h ${pad(mi)}min ${pad(s)}s`,
                titulo: `${th} h ${pad(mi)} min para ${alvo.nome}`
            };
        }

        if (ms > 0) {
            const relogio = `${pad(mi)}:${pad(s)}`;
            return {
                chave: `m${relogio}`,
                partes: [[relogio, '']],
                exact: 'Começa à meia-noite.',
                titulo: `${relogio} para ${alvo.nome}`
            };
        }

        if (agora < alvo.fimFolga) {
            const noFeriado = agora < alvo.fimFeriado;
            const ultimoDia = iso(agora) === alvo.folgaFimIso && !alvo.fimDeSemana;
            return {
                chave: noFeriado ? 'hoje' : 'folga',
                emFolga: true,
                etiqueta: noFeriado ? 'Hoje é feriado' : 'Você está de folga',
                nota: ultimoDia ? 'Aproveite o dia.' : alvo.nota,
                titulo: noFeriado ? `Hoje é ${alvo.nome}` : 'Você está de folga'
            };
        }

        return { chave: 'passou', etiqueta: 'Já passou', titulo: tituloOriginal };
    }

    function marcarHoje(agora) {
        const hojeIso = iso(agora);
        section.querySelectorAll('.strip-days li')
            .forEach(li => li.classList.toggle('today', li.dataset.iso === hojeIso));
    }

    function render() {
        if (!alvo) return;

        const agora = new Date();
        const e = estado(agora);

        if (e.relogio) {
            if (relogio.raiz.parentNode !== el.exact) el.exact.replaceChildren(relogio.raiz);
            e.relogio.forEach(([n], i) => { relogio.numeros[i].textContent = n; });
        } else {
            el.exact.textContent = e.exact ?? '';
        }
        el.exact.classList.toggle('is-clock', !!e.relogio);
        // Fica visível (com altura reservada no CSS) enquanto há contagem, para não empurrar a página.
        el.exact.hidden = !e.partes;

        if (e.chave !== ultimaChave) {
            // Página em cache pode trazer como próxima uma folga que já acabou: pede a seguinte.
            if (e.chave === 'passou' && alvo.proxima && api.aoPassar?.()) return;

            ultimaChave = e.chave;

            const filhos = e.partes ? e.partes.map(([n, u]) => parte(n, u)) : [etiqueta(e.etiqueta)];
            el.count.replaceChildren(...filhos);

            el.note.textContent = e.nota ?? '';
            el.note.hidden = !e.nota;

            el.kicker.textContent = !alvo.proxima ? 'Feriado escolhido'
                : e.emFolga ? 'Sua folga começou' : 'Sua próxima folga';
            document.body.classList.toggle('is-holiday', !!e.emFolga);
            document.title = e.titulo;
            marcarHoje(agora);
        }
    }

    function start() {
        if (intervalo !== null) return;
        render();
        intervalo = setInterval(render, 1000);
    }

    function stop() {
        clearInterval(intervalo);
        intervalo = null;
    }

    /** aoPassar: chamado quando a próxima folga acabou; devolve true se trocou o alvo. */
    const api = { definir, start, stop, render, aoPassar: null };
    return api;
}
