export async function playRelease(button) {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        return;
    }

    const animation = button.animate([
        { transform: 'translateY(1px) scale(0.92)' },
        { transform: 'translateY(-1px) scale(1.04)', offset: 0.6 },
        { transform: 'translateY(0) scale(1)' }
    ], { duration: 240, easing: 'ease-out' });

    await animation.finished;
}
