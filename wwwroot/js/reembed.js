// reembed.js — batch re-embedding progress for Reembed/Index view

(function () {
    const dataEl = document.getElementById('reembed-data');
    const projectId = dataEl?.dataset.projectId || '';
    const processBatchUrl = dataEl?.dataset.processBatchUrl || '';

    let running = false;

    window.startReembed = async function () {
        if (running) return;
        running = true;
        document.getElementById('btn-start').disabled = true;
        document.getElementById('status-msg').textContent = 'Bezig\u2026';
        processBatch();
    };

    async function processBatch() {
        try {
            const params = new URLSearchParams({ batchSize: 50 });
            if (projectId) params.append('projectId', projectId);

            const resp = await fetch(processBatchUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? ''
                },
                body: params
            });

            const data = await resp.json();
            if (data.error) {
                document.getElementById('status-msg').textContent = 'Fout: ' + data.error;
                document.getElementById('btn-start').disabled = false;
                running = false;
                return;
            }

            const pct = data.total > 0 ? Math.round(data.processed * 100 / data.total) : 0;
            document.getElementById('count-text').textContent = data.processed + ' / ' + data.total;
            document.getElementById('pct').textContent = pct;
            document.getElementById('progress-bar').style.width = pct + '%';

            if (data.done) {
                document.getElementById('status-msg').textContent = 'Klaar!';
                document.getElementById('btn-start').textContent = 'Klaar \u2713';
            } else {
                setTimeout(processBatch, 200);
            }
        } catch (e) {
            document.getElementById('status-msg').textContent = 'Netwerkfout: ' + e.message;
            document.getElementById('btn-start').disabled = false;
            running = false;
        }
    }
})();
