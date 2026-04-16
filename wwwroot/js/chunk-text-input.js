// chunk-text-input.js — text input page logic (word count, folders, placeholder management)
// Requires placeholder-autocomplete.js to be loaded first

(function () {
    const dataEl = document.getElementById('chunk-form-data');
    const koppelingPerProject = JSON.parse(dataEl?.dataset.koppelingPerProject || '{}');
    const initialProjectIdRaw = dataEl?.dataset.initialProjectId;
    const initialProjectId = initialProjectIdRaw ? parseInt(initialProjectIdRaw) : null;
    const initialFolderData = JSON.parse(dataEl?.dataset.initialFolderData || '[]');

    function updateLinkFieldVisibility(projectId) {
        const enabled = koppelingPerProject[projectId] === true;
        document.getElementById('linkField').style.display = enabled ? '' : 'none';
    }

    if (initialProjectId !== null) {
        updateLinkFieldVisibility(initialProjectId);
    }

    function buildFolderOptions(folderData, select, parentId, depth) {
        parentId = parentId ?? null;
        depth = depth ?? 0;
        const children = folderData
            .filter(f => f.parentId === parentId)
            .sort((a, b) => a.naam.localeCompare(b.naam, 'nl'));
        for (const f of children) {
            const opt = document.createElement('option');
            opt.value = f.id;
            opt.textContent = '\u00a0\u00a0'.repeat(depth * 2) + (depth > 0 ? '\u2514 ' : '') + f.naam;
            select.appendChild(opt);
            buildFolderOptions(folderData, select, f.id, depth + 1);
        }
    }

    buildFolderOptions(initialFolderData, document.getElementById('folderId'));

    const projectSelect = document.getElementById('projectId');
    if (projectSelect) {
        updateLinkFieldVisibility(parseInt(projectSelect.value));
        projectSelect.addEventListener('change', async function () {
            updateLinkFieldVisibility(parseInt(this.value));
            const sel = document.getElementById('folderId');
            sel.innerHTML = '<option value="">\u2014 Geen map \u2014</option>';
            if (!this.value) return;
            const resp = await fetch('/Chunk/GetFolders?projectId=' + this.value);
            const folders = await resp.json();
            buildFolderOptions(folders, sel);
        });
    }

    // Word count
    const textarea = document.getElementById('text');
    const wordCountEl = document.getElementById('wordCount');
    function updateWordCount() {
        const words = textarea.value.trim().split(/\s+/).filter(w => w.length > 0);
        wordCountEl.textContent = words.length.toLocaleString('nl-NL') + ' woorden';
    }
    textarea.addEventListener('input', updateWordCount);
    updateWordCount();

    // Spinner on submit
    document.getElementById('textInputForm').addEventListener('submit', function () {
        document.getElementById('btnNormal').classList.add('d-none');
        document.getElementById('btnSpinner').classList.remove('d-none');
        document.getElementById('submitBtn').disabled = true;
    });

    // ── Placeholder management ──────────────────────────────────────────────────

    const afToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    async function apiPost(url, params) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ __RequestVerificationToken: afToken, ...params })
        });
        return resp.json();
    }

    let allPlaceholders = [];
    let currentProjectId = initialProjectId;

    async function loadPlaceholders() {
        if (!currentProjectId) return;
        const resp = await fetch(`/Chunk/GetPlaceholders?projectId=${currentProjectId}`);
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

        const btn = this;
        btn.dataset.originalHtml = btn.innerHTML;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Bezig\u2026';
        btn.disabled = true;

        const data = await apiPost('/Chunk/SavePlaceholder', { id, projectId: currentProjectId, naam, waarde });

        btn.innerHTML = btn.dataset.originalHtml;
        btn.disabled = false;

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

    if (projectSelect) {
        projectSelect.addEventListener('change', async function () {
            currentProjectId = parseInt(this.value) || null;
            const btn = document.getElementById('btnVariabelen');
            btn.disabled = !currentProjectId;
            allPlaceholders = [];
            document.getElementById('placeholderCount').textContent = '\u2026';
            autocomplete.updateList([]);
            if (currentProjectId) await loadPlaceholders();
        });
    }

    if (initialProjectId !== null) loadPlaceholders();

    // ── @@ autocomplete ─────────────────────────────────────────────────────────
    const autocomplete = new PlaceholderAutocomplete();
    autocomplete.attach(textarea);
})();
