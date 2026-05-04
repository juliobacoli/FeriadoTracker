import { startConfetti, attachConfettiButton } from './confetti.js';
import { createCountdown } from './countdown.js';
import { attachTimelineHandlers } from './timeline.js';

document.addEventListener('DOMContentLoaded', function () {
    const wrapper = document.querySelector('.holiday-wrapper');
    if (!wrapper) return;

    const elements = {
        days: document.getElementById('days'),
        hours: document.getElementById('hours'),
        minutes: document.getElementById('minutes'),
        seconds: document.getElementById('seconds'),
        card: document.querySelector('.holiday-card'),
        title: document.querySelector('.holiday-title'),
        subtitle: document.querySelector('.holiday-subtitle'),
        date: document.querySelector('.holiday-date')
    };

    const confettiBtn = document.getElementById('confettiToggle');
    attachConfettiButton(confettiBtn);

    const countdown = createCountdown({
        elements,
        onZero() {
            if (confettiBtn) confettiBtn.hidden = false;
            startConfetti();
        }
    });

    const timelineItems = document.querySelectorAll('.holiday-timeline-item');
    attachTimelineHandlers({
        items: timelineItems,
        elements,
        confettiBtn,
        countdown
    });

    const initialDateString = wrapper.getAttribute('data-initial-date');
    if (initialDateString) {
        countdown.setDate(new Date(initialDateString));
    }
    countdown.start();
});
