// conversatie-detail.js — correction modal for conversation detail view

(function () {
    const modal      = new bootstrap.Modal(document.getElementById('correctieModal'));
    const elOriginal = document.getElementById('cm-original');
    const screens    = {
        decision:   document.getElementById('cm-screen-decision'),
        factual:    document.getElementById('cm-screen-factual'),
        behavioral: document.getElementById('cm-screen-behavioral'),
    };

    function showScreen(name) {
        Object.values(screens).forEach(s => s.style.display = 'none');
        screens[name].style.display = '';
    }

    window.openCorrectieModal = function (btn) {
        elOriginal.textContent = btn.dataset.messageText;
        document.getElementById('cm-message-id').value      = btn.dataset.messageId;
        document.getElementById('cm-correctie-tekst').value = '';
        document.getElementById('cm-btn-back-factual').style.display = '';

        showScreen('decision');
        modal.show();
    };

    document.getElementById('cm-btn-factual').addEventListener('click',
        () => showScreen('factual'));

    document.getElementById('cm-btn-behavioral').addEventListener('click',
        () => showScreen('behavioral'));

    document.getElementById('cm-btn-back-factual').addEventListener('click',
        () => showScreen('decision'));

    document.getElementById('cm-btn-back-behavioral').addEventListener('click',
        () => showScreen('decision'));

    const submitBtn  = document.querySelector('#cm-factual-form button[type="submit"]');
    const submitIcon = submitBtn.querySelector('i');

    document.getElementById('cm-factual-form').addEventListener('submit', async function (e) {
        e.preventDefault();

        const correctieTekst = document.getElementById('cm-correctie-tekst').value.trim();
        if (!correctieTekst) return;

        submitBtn.disabled = true;
        submitIcon.className = 'bi bi-hourglass-split me-1';

        const form     = document.getElementById('cm-factual-form');
        const postUrl  = form.dataset.postUrl;
        const formData = new FormData(form);
        const token    = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.set('__RequestVerificationToken', token);

        try {
            const response = await fetch(postUrl, { method: 'POST', body: formData });

            if (response.ok) {
                submitIcon.className = 'bi bi-check-lg me-1';
                submitBtn.textContent = '';
                submitBtn.appendChild(document.createElement('i')).className = 'bi bi-check-lg me-1';
                submitBtn.append('Opgeslagen');
                setTimeout(() => modal.hide(), 800);
            } else {
                submitIcon.className = 'bi bi-exclamation-triangle me-1';
                submitBtn.disabled = false;
            }
        } catch {
            submitIcon.className = 'bi bi-exclamation-triangle me-1';
            submitBtn.disabled = false;
        }
    });
})();
