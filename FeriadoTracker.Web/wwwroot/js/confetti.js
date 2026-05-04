export function startConfetti() {
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

        window.confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.1, 0.3), y: -0.1 }
        });

        window.confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.7, 0.9), y: -0.1 }
        });

    }, 180);
}

export function attachConfettiButton(button, cooldownMs = 3000) {
    if (!button) return;

    let onCooldown = false;

    button.addEventListener('click', function () {
        if (onCooldown) return;

        onCooldown = true;
        button.disabled = true;
        button.classList.add('is-clicked');

        startConfetti();

        setTimeout(function () {
            onCooldown = false;
            button.disabled = false;
        }, cooldownMs);
    });
}
