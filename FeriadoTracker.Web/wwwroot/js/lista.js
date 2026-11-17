/**
 * Troca a lista de feriados pelos locais marcados, sem recarregar a página.
 * O servidor devolve só o trecho da lista (/?handler=Lista&local=...), montado
 * pela mesma partial da página. Depois da troca, avisa com o evento 'lista-atualizada'
 * (detail: proxima e kicker) para o destaque acompanhar a nova lista.
 */
let controle = null;

export async function atualizarLista(codigos) {
    // Só a resposta do último pedido vale: um toque novo cancela o anterior.
    controle?.abort();
    const atualControle = new AbortController();
    controle = atualControle;

    const params = new URLSearchParams({ handler: 'Lista' });
    codigos.forEach(c => params.append('local', c));

    try {
        const res = await fetch('/?' + params, { signal: atualControle.signal });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const html = await res.text();

        const atual = document.getElementById('lista');
        const nova = new DOMParser().parseFromString(html, 'text/html').getElementById('lista');
        if (!atual || !nova) throw new Error('Lista ausente na resposta');

        atual.replaceWith(nova);
        document.dispatchEvent(new CustomEvent('lista-atualizada', {
            detail: { proxima: Number(nova.dataset.proxima), kicker: nova.dataset.kicker }
        }));
    } catch (err) {
        if (err.name === 'AbortError') return;
        throw err;
    } finally {
        if (controle === atualControle) controle = null;
    }
}
