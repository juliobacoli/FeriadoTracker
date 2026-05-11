export function setupShareButton(btn, elements) {
    // Se o navegador não suporta a Web Share API, nem mostramos o botão
    if (!navigator.share) {
        return;
    }

    // Navegador suporta! Exibimos o botão
    btn.hidden = false;

    btn.addEventListener('click', async () => {
        try {
            const title = elements.title.innerText;
            const daysText = elements.days.innerText;
            const daysInt = parseInt(daysText, 10);
            
            let text = '';
            
            // Monta a frase dinamicamente baseada nos dias restantes da tela
            if (isNaN(daysInt) || daysInt <= 0) {
                text = `Hoje é ${title}! 🎉 Veja no FeriadoTracker.`;
            } else if (daysInt === 1) {
                text = `Amanhã é ${title}! 🚀 Veja no FeriadoTracker.`;
            } else {
                text = `Faltam ${daysInt} dias para ${title}! ⏳ Veja no FeriadoTracker.`;
            }

            await navigator.share({
                title: 'FeriadoTracker',
                text: text,
                url: window.location.origin
            });
        } catch (err) {
            // Ignora erro se o usuário apenas fechou/cancelou a gaveta nativa
            if (err.name !== 'AbortError') {
                console.error('Erro ao compartilhar:', err);
            }
        }
    });
}
