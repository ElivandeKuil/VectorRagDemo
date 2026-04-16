// chunk-upload.js — document upload page logic

(function () {
    const dataEl = document.getElementById('chunk-form-data');
    const koppelingPerProject = JSON.parse(dataEl?.dataset.koppelingPerProject || '{}');
    const initialProjectId = dataEl?.dataset.initialProjectId
        ? parseInt(dataEl.dataset.initialProjectId)
        : null;
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

    document.getElementById('uploadForm').addEventListener('submit', function () {
        document.getElementById('btnNormal').classList.add('d-none');
        document.getElementById('btnSpinner').classList.remove('d-none');
        document.getElementById('submitBtn').disabled = true;
    });
})();
