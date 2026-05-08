export function attachTimelineHandlers({ items, elements, confettiBtn, countdown }) {
    items.forEach(item => {
        item.addEventListener('click', function () {
            const newDateStr = this.getAttribute('data-target-date');
            const newName = this.getAttribute('data-target-name');
            const newFormatted = this.getAttribute('data-target-formatted');
            const isNext = this.classList.contains('is-next');

            if (elements.title) elements.title.textContent = newName;
            if (elements.date) elements.date.textContent = newFormatted;
            if (elements.subtitle) {
                elements.subtitle.textContent = isNext ? 'PRÓXIMO FERIADO NACIONAL' : 'FERIADO SELECIONADO';
            }

            if (confettiBtn) {
                confettiBtn.hidden = true;
                confettiBtn.classList.remove('is-clicked');
            }

            items.forEach(i => i.classList.remove('is-selected'));
            this.classList.add('is-selected');

            countdown.setDate(new Date(newDateStr));
            countdown.start();
        });
    });
}
