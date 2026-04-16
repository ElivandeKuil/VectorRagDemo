// chunk-edit.js — chunk editing page (save/delete/add chunks, placeholder management)
// Requires placeholder-autocomplete.js to be loaded first

(function () {
    const dataEl = document.getElementById('chunk-edit-data');
    const bronId = parseInt(dataEl?.dataset.bronId || '0');
    const projectId = parseInt(dataEl?.dataset.projectId || '0');
    const afToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // ── Shared helpers ──────────────────────────────────────────────────────────

    async function apiPost(url, params) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ __RequestVerificationToken: afToken, ...params })
        });
        return resp.json();
    }

    function setStatus(chunkId, type, msg) {
        const el = document.getElementById('status-' + chunkId);
        el.className = `card-footer py-1 px-3 small bg-${type === 'ok' ? 'success' : type === 'err' ? 'danger' : 'warning'} bg-opacity-10 text-${type === 'ok' ? 'success' : type === 'err' ? 'danger' : 'warning'}`;
        el.textContent = msg;
        el.classList.remove('d-none');
        if (type === 'ok') setTimeout(() => el.classList.add('d-none'), 3000);
    }

    function setLoading(btn, loading) {
        if (loading) {
            btn.dataset.originalHtml = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Bezig\u2026';
            btn.disabled = true;
        } else {
            btn.innerHTML = btn.dataset.originalHtml ?? btn.innerHTML;
            btn.disabled = false;
        }
    }

    function updateWordCount(chunkId, textarea) {
        const wc = textarea.value.trim().split(/\s+/).filter(Boolean).length;
        const el = document.getElementById('wc-' + chunkId);
        if (el) el.textContent = wc + ' woorden';
    }

    // ── Chunk save / delete / add ───────────────────────────────────────────────

    document.querySelectorAll('.btn-save-chunk').forEach(btn => {
        btn.addEventListener('click', async function () {
            const chunkId = this.dataset.chunkId;
            const tekst = document.getElementById('tekst-' + chunkId).value;
            if (!tekst.trim()) { setStatus(chunkId, 'err', 'Tekst mag niet leeg zijn.'); return; }
            setLoading(this, true);
            setStatus(chunkId, 'busy', 'Opslaan en embedden\u2026');
            const data = await apiPost('/Chunk/UpdateChunk', { chunkId, tekst });
            setLoading(this, false);
            data.success ? setStatus(chunkId, 'ok', 'Opgeslagen en embedding vernieuwd.')
                         : setStatus(chunkId, 'err', data.message ?? 'Opslaan mislukt.');
        });
    });

    document.querySelectorAll('.btn-delete-chunk').forEach(btn => {
        btn.addEventListener('click', async function () {
            const chunkId = this.dataset.chunkId;
            if (!confirm('Chunk verwijderen?')) return;
            setLoading(this, true);
            const data = await apiPost('/Chunk/DeleteChunk', { chunkId });
            if (data.success) { document.getElementById('chunk-card-' + chunkId).remove(); renumberChunks(); }
            else { setLoading(this, false); alert(data.message ?? 'Verwijderen mislukt.'); }
        });
    });

    document.querySelectorAll('.chunk-textarea').forEach(ta => {
        const chunkId = ta.id.replace('tekst-', '');
        ta.addEventListener('input', () => updateWordCount(chunkId, ta));
    });

    function renumberChunks() {
        document.querySelectorAll('.chunk-card').forEach((card, idx) => {
            card.querySelector('.card-header .text-muted').textContent = 'Chunk ' + (idx + 1);
        });
    }

    const newChunkTextarea = document.getElementById('newChunkText');
    newChunkTextarea.addEventListener('input', function () {
        const wc = this.value.trim().split(/\s+/).filter(Boolean).length;
        document.getElementById('newChunkWc').textContent = wc + ' woorden';
    });

    document.getElementById('btnAddChunk').addEventListener('click', async function () {
        const tekst = newChunkTextarea.value;
        if (!tekst.trim()) { alert('Voer tekst in voor de nieuwe chunk.'); return; }
        setLoading(this, true);
        const data = await apiPost('/Chunk/AddChunk', { bronId, tekst });
        setLoading(this, false);
        if (data.success) {
            newChunkTextarea.value = '';
            document.getElementById('newChunkWc').textContent = '0 woorden';
            const chunkList = document.getElementById('chunkList');
            const idx = chunkList.querySelectorAll('.chunk-card').length + 1;
            const chunkId = data.chunkId;
            const card = document.createElement('div');
            card.className = 'card mb-3 chunk-card';
            card.id = 'chunk-card-' + chunkId;
            card.dataset.chunkId = chunkId;
            card.innerHTML = `
                <div class="card-header d-flex align-items-center justify-content-between py-2">
                    <span class="text-muted small fw-semibold">Chunk ${idx}</span>
                    <div class="d-flex align-items-center gap-2">
                        <span class="badge bg-light text-muted small chunk-word-count" id="wc-${chunkId}">
                            ${tekst.trim().split(/\s+/).filter(Boolean).length} woorden
                        </span>
                        <button type="button" class="btn btn-sm btn-outline-primary btn-save-chunk" data-chunk-id="${chunkId}" title="Opslaan en embedding vernieuwen">
                            <i class="bi bi-floppy me-1"></i>Opslaan
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger btn-delete-chunk" data-chunk-id="${chunkId}" title="Chunk verwijderen">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="card-body p-0">
                    <textarea class="form-control border-0 rounded-0 chunk-textarea" id="tekst-${chunkId}" rows="6"
                              style="resize:vertical;font-size:.85rem;line-height:1.65;font-family:inherit">${escapeHtml(tekst.trim())}</textarea>
                </div>
                <div class="card-footer py-1 px-3 d-none chunk-status" id="status-${chunkId}"></div>`;
            chunkList.appendChild(card);

            card.querySelector('.btn-save-chunk').addEventListener('click', async function () {
                const t = document.getElementById('tekst-' + chunkId).value;
                if (!t.trim()) { setStatus(chunkId, 'err', 'Tekst mag niet leeg zijn.'); return; }
                setLoading(this, true);
                setStatus(chunkId, 'busy', 'Opslaan en embedden\u2026');
                const r = await apiPost('/Chunk/UpdateChunk', { chunkId, tekst: t });
                setLoading(this, false);
                r.success ? setStatus(chunkId, 'ok', 'Opgeslagen en embedding vernieuwd.')
                          : setStatus(chunkId, 'err', r.message ?? 'Opslaan mislukt.');
            });
            card.querySelector('.btn-delete-chunk').addEventListener('click', async function () {
                if (!confirm('Chunk verwijderen?')) return;
                setLoading(this, true);
                const r = await apiPost('/Chunk/DeleteChunk', { chunkId });
                if (r.success) { card.remove(); renumberChunks(); }
                else { setLoading(this, false); alert(r.message ?? 'Verwijderen mislukt.'); }
            });
            const newTa = card.querySelector('.chunk-textarea');
            newTa.addEventListener('input', () => updateWordCount(chunkId, newTa));
            autocomplete.attach(newTa);
        } else {
            alert(data.message ?? 'Toevoegen mislukt.');
        }
    });

    // ── Placeholder management ──────────────────────────────────────────────────

    let allPlaceholders = [];

    async function loadPlaceholders() {
        const resp = await fetch(`/Chunk/GetPlaceholders?projectId=${projectId}`);
        allPlaceholders = await resp.json();
        renderPlaceholderList();
        document.getElementById('placeholderCount').textContent = allPlaceholders.length;
        autocomplete.updateList(allPlaceholders);
    }

    function renderPlaceholderList() {
        const list = document.getElementById('placeholderList');
        if (allPlaceholders.length === 0) {
            list.innerHTML = '<div class="text-muted small text-center py-2">Nog geen variabelen.</div>';
            return;
        }
        list.innerHTML = allPlaceholders.map(p => `
            <div class="ph-item d-flex align-items-start gap-2 py-1 px-2 rounded" data-id="${p.id}">
                <div class="flex-grow-1 min-w-0">
                    <div class="fw-semibold text-primary small">@@${escapeHtml(p.naam)}</div>
                    <div class="text-muted small text-truncate" title="${escapeHtml(p.waarde)}">${escapeHtml(p.waarde)}</div>
                </div>
                <div class="d-flex gap-1 flex-shrink-0 pt-1">
                    <button class="btn-ph btn-ph-edit" data-id="${p.id}" title="Bewerken"><i class="bi bi-pencil"></i></button>
                    <button class="btn-ph btn-ph-del" data-id="${p.id}" title="Verwijderen"><i class="bi bi-trash"></i></button>
                </div>
            </div>`).join('');

        list.querySelectorAll('.btn-ph-edit').forEach(btn => btn.addEventListener('click', () => startEdit(parseInt(btn.dataset.id))));
        list.querySelectorAll('.btn-ph-del').forEach(btn => btn.addEventListener('click', () => deletePlaceholder(parseInt(btn.dataset.id))));
    }

    function startEdit(id) {
        const ph = allPlaceholders.find(p => p.id === id);
        if (!ph) return;
        document.getElementById('phEditId').value = ph.id;
        document.getElementById('phNaam').value = ph.naam;
        document.getElementById('phWaarde').value = ph.waarde;
        document.getElementById('phFormTitle').textContent = 'Variabele bewerken';
        document.getElementById('btnCancelEdit').classList.remove('d-none');
        document.getElementById('phNaam').focus();
    }

    function resetForm() {
        document.getElementById('phEditId').value = '0';
        document.getElementById('phNaam').value = '';
        document.getElementById('phWaarde').value = '';
        document.getElementById('phFormTitle').textContent = 'Nieuwe variabele';
        document.getElementById('btnCancelEdit').classList.add('d-none');
        document.getElementById('phFormError').classList.add('d-none');
    }

    document.getElementById('btnCancelEdit').addEventListener('click', resetForm);

    document.getElementById('btnSavePlaceholder').addEventListener('click', async function () {
        const id = parseInt(document.getElementById('phEditId').value);
        const naam = document.getElementById('phNaam').value.trim();
        const waarde = document.getElementById('phWaarde').value.trim();
        const errEl = document.getElementById('phFormError');
        errEl.classList.add('d-none');

        setLoading(this, true);
        const data = await apiPost('/Chunk/SavePlaceholder', { id, projectId, naam, waarde });
        setLoading(this, false);

        if (data.success) {
            resetForm();
            await loadPlaceholders();
        } else {
            errEl.textContent = data.message ?? 'Opslaan mislukt.';
            errEl.classList.remove('d-none');
        }
    });

    async function deletePlaceholder(id) {
        if (!confirm('Variabele verwijderen?')) return;
        const data = await apiPost('/Chunk/DeletePlaceholder', { id });
        if (data.success) {
            await loadPlaceholders();
        } else {
            alert(data.message ?? 'Verwijderen mislukt.');
        }
    }

    loadPlaceholders();

    // ── @@ autocomplete ─────────────────────────────────────────────────────────
    const autocomplete = new PlaceholderAutocomplete();
    document.querySelectorAll('.chunk-textarea, #newChunkText').forEach(ta => autocomplete.attach(ta));

    // ── Rename document ───────────────────────────────────────────────────────
    document.querySelectorAll('.btn-rename-doc').forEach(btn => {
        btn.addEventListener('click', function () {
            document.getElementById('renameDocInput').value = this.dataset.currentTitle;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('renameDocModal')).show();
            setTimeout(() => document.getElementById('renameDocInput').select(), 300);
        });
    });

    document.getElementById('renameDocForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const title = document.getElementById('renameDocInput').value.trim();
        if (!title) return;
        const data = await apiPost('/Chunk/RenameDocument', { id: bronId, title });
        if (data.success) {
            document.getElementById('docTitle').textContent = title;
            document.querySelector('.btn-rename-doc').dataset.currentTitle = title;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('renameDocModal')).hide();
        } else {
            alert(data.message ?? 'Hernoemen mislukt.');
        }
    });
})();
