/**
 * Gerenciamento do contador de feriados e interações da timeline
 */

// Constantes de conversão de tempo (Passo 3)
const MS = {
    SECOND: 1000,
    MINUTE: 60 * 1000,
    HOUR: 60 * 60 * 1000,
    DAY: 24 * 60 * 60 * 1000
};

function formatNumber(num) {
    return String(num).padStart(2, '0');
}

function isValidDate(date) {
    return date instanceof Date && !isNaN(date);
}

function startConfetti() {
    const duration = 4000;
    const animationEnd = Date.now() + duration;

    const defaults = {
        startVelocity: 22,
        spread: 360,
        ticks: 90,
        zIndex: 999,
        scalar: 1.0,
        useWorker: false
    };

    function randomInRange(min, max) {
        return Math.random() * (max - min) + min;
    }

    const interval = setInterval(function () {
        const timeLeft = animationEnd - Date.now();

        if (timeLeft <= 0) {
            return clearInterval(interval);
        }

        const particleCount = 40 * (timeLeft / duration);

        confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.1, 0.3), y: -0.1 }
        });

        confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.7, 0.9), y: -0.1 }
        });

    }, 180);
}

document.addEventListener('DOMContentLoaded', function () {
    const wrapper = document.querySelector('.holiday-wrapper');
    if (!wrapper) return;

    // Cache de Elementos (Passo 2)
    const elements = {
        days: document.getElementById('days'),
        hours: document.getElementById('hours'),
        minutes: document.getElementById('minutes'),
        seconds: document.getElementById('seconds'),
        card: document.querySelector('.holiday-card'),
        title: document.querySelector('.holiday-title'),
        subtitle: document.querySelector('.holiday-subtitle'),
        date: document.querySelector('.holiday-date'),
        confettiBtn: document.getElementById('confettiToggle')
    };

    let confettiCooldown = false;

    if (elements.confettiBtn) {
        elements.confettiBtn.addEventListener('click', function () {
            if (confettiCooldown) return;

            confettiCooldown = true;
            elements.confettiBtn.disabled = true;
            elements.confettiBtn.classList.add('is-clicked');

            startConfetti();

            setTimeout(function () {
                confettiCooldown = false;
                elements.confettiBtn.disabled = false;
            }, 3000);
        });
    }

    const initialDateString = wrapper.getAttribute('data-initial-date');
    let activeDate = initialDateString ? new Date(initialDateString) : null;
    let countdownInterval = null;

    function updateCountdown() {
        if (!isValidDate(activeDate)) {
            if (elements.date) elements.date.textContent = "Informação indisponível";
            if (elements.title) elements.title.textContent = "Data não encontrada";
            
            ['days', 'hours', 'minutes', 'seconds'].forEach(key => {
                if (elements[key]) elements[key].textContent = '00';
            });
            return;
        }

        const now = new Date();
        const diff = activeDate - now;

        if (diff <= 0) {
            ['days', 'hours', 'minutes', 'seconds'].forEach(key => {
                if (elements[key]) elements[key].textContent = '00';
            });

            if (elements.date) elements.date.textContent = 'Hoje é feriado! Aproveite!';
            if (elements.card) elements.card.classList.add('holiday-today');
            if (elements.confettiBtn) elements.confettiBtn.hidden = false;

            startConfetti();

            if (countdownInterval !== null) {
                clearInterval(countdownInterval);
                countdownInterval = null;
            }
            return;
        }

        if (elements.card) elements.card.classList.remove('holiday-today');
        if (elements.confettiBtn) elements.confettiBtn.hidden = true;

        const days = Math.floor(diff / MS.DAY);
        const hours = Math.floor((diff % MS.DAY) / MS.HOUR);
        const minutes = Math.floor((diff % MS.HOUR) / MS.MINUTE);
        const seconds = Math.floor((diff % MS.MINUTE) / MS.SECOND);

        if (elements.days) elements.days.textContent = formatNumber(days);
        if (elements.hours) elements.hours.textContent = formatNumber(hours);
        if (elements.minutes) elements.minutes.textContent = formatNumber(minutes);
        if (elements.seconds) elements.seconds.textContent = formatNumber(seconds);
    }


    // Timeline Clicks
    document.querySelectorAll('.holiday-timeline-item').forEach(item => {
        item.addEventListener('click', function () {
            const newDateStr = this.getAttribute('data-target-date');
            const newName = this.getAttribute('data-target-name');
            const newFormatted = this.getAttribute('data-target-formatted');
            const isNext = this.classList.contains('is-next');

            activeDate = new Date(newDateStr);

            if (elements.title) elements.title.textContent = newName;
            if (elements.date) elements.date.textContent = newFormatted;
            if (elements.subtitle) {
                elements.subtitle.textContent = isNext ? 'PRÓXIMO FERIADO NACIONAL' : 'FERIADO SELECIONADO';
            }

            if (elements.confettiBtn) {
                elements.confettiBtn.hidden = true;
                elements.confettiBtn.classList.remove('is-clicked');
            }

            document.querySelectorAll('.holiday-timeline-item').forEach(i => i.classList.remove('is-selected'));
            this.classList.add('is-selected');

            if (countdownInterval === null) {
                countdownInterval = setInterval(updateCountdown, 1000);
            }
            updateCountdown();
        });
    });

    updateCountdown();
    countdownInterval = setInterval(updateCountdown, 1000);
});
