/**
 * Rolagem suave com Lenis (wwwroot/lib/lenis, MIT), no desktop e no celular.
 * Com "reduzir movimento", ou se o arquivo não carregar, vale a rolagem nativa.
 */
const semMovimento = window.matchMedia('(prefers-reduced-motion: reduce)');
let lenis = null;

export function iniciarRolagem() {
    if (semMovimento.matches) return;

    // Carregado depois da página: a rolagem nativa funciona até ele chegar.
    const script = document.createElement('script');
    script.src = '/lib/lenis/lenis.min.js';
    script.onload = () => {
        // syncTouch estende a suavização ao toque; autoRaf dispensa laço próprio.
        // lerp e syncTouchLerp abaixo do padrão (0.1 e 0.075): a página desliza mais antes de parar.
        lenis = new window.Lenis({ autoRaf: true, syncTouch: true, lerp: 0.06, syncTouchLerp: 0.05 });
    };
    document.head.appendChild(script);
}

/** Rola até o elemento parar logo abaixo do cabeçalho fixo, suave quando permitido. */
export function rolarAte(elemento) {
    const cabecalho = document.querySelector('.topbar')?.offsetHeight ?? 0;
    // Alvo em pixels a partir da rolagem real: logo depois de um arrasto, o estado interno do Lenis pode estar atrasado.
    const alvo = Math.max(0, Math.round(elemento.getBoundingClientRect().top + window.scrollY - cabecalho - 8));

    if (lenis) lenis.scrollTo(alvo);
    else window.scrollTo({ top: alvo, behavior: semMovimento.matches ? 'auto' : 'smooth' });
}
