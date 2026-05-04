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

export function createCountdown({ elements, onZero }) {
    let activeDate = null;
    let intervalId = null;

    function render() {
        if (!isValidDate(activeDate)) {
            if (elements.date) elements.date.textContent = "Informação indisponível";
            if (elements.title) elements.title.textContent = "Data não encontrada";
            ['days', 'hours', 'minutes', 'seconds'].forEach(key => {
                if (elements[key]) elements[key].textContent = '00';
            });
            return;
        }

        const diff = activeDate - new Date();

        if (diff <= 0) {
            ['days', 'hours', 'minutes', 'seconds'].forEach(key => {
                if (elements[key]) elements[key].textContent = '00';
            });
            if (elements.date) elements.date.textContent = 'Hoje é feriado! Aproveite!';
            if (elements.card) elements.card.classList.add('holiday-today');

            if (typeof onZero === 'function') onZero();
            stop();
            return;
        }

        if (elements.card) elements.card.classList.remove('holiday-today');

        const days = Math.floor(diff / MS.DAY);
        const hours = Math.floor((diff % MS.DAY) / MS.HOUR);
        const minutes = Math.floor((diff % MS.HOUR) / MS.MINUTE);
        const seconds = Math.floor((diff % MS.MINUTE) / MS.SECOND);

        if (elements.days) elements.days.textContent = formatNumber(days);
        if (elements.hours) elements.hours.textContent = formatNumber(hours);
        if (elements.minutes) elements.minutes.textContent = formatNumber(minutes);
        if (elements.seconds) elements.seconds.textContent = formatNumber(seconds);
    }

    function start() {
        if (intervalId !== null) return;
        intervalId = setInterval(render, 1000);
        render();
    }

    function stop() {
        if (intervalId === null) return;
        clearInterval(intervalId);
        intervalId = null;
    }

    function setDate(newDate) {
        activeDate = newDate;
        render();
    }

    return { start, stop, setDate, render };
}
