export function setupShareButton(btn, elements) {
    btn.hidden = false;

    btn.addEventListener('click', async () => {
        if (navigator.share) {
            try {
                const title = elements.title.innerText;
                const daysText = elements.days.innerText;
                const daysInt = parseInt(daysText, 10);

                let text = '';

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
                if (err.name !== 'AbortError') {
                    console.error('Erro ao compartilhar:', err);
                }
            }
        } else {
            try {
                await navigator.clipboard.writeText(window.location.href);
                const original = btn.innerHTML;
                btn.innerHTML = '✓ Copiado!';
                btn.disabled = true;
                setTimeout(() => {
                    btn.innerHTML = original;
                    btn.disabled = false;
                }, 2000);
            } catch (err) {
                console.error('Erro ao copiar link:', err);
            }
        }
    });
}
