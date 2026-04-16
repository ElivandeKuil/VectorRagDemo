// chunk-index.js — document index page (folder tree, drag-drop, modals, search, preview)

(function () {
    const dataEl = document.getElementById('chunk-index-data');
    const folderData = JSON.parse(dataEl?.dataset.folderData || '[]');
    const currentFolderIdRaw = dataEl?.dataset.currentFolderId;
    const currentFolderId = currentFolderIdRaw ? parseInt(currentFolderIdRaw) : null;
    const projectId = parseInt(dataEl?.dataset.projectId || '0');
    const totalDocCount = parseInt(dataEl?.dataset.totalDocCount || '0');
    const deleteActionBase = dataEl?.dataset.deleteUrl || '';
    const previewUrl = dataEl?.dataset.previewUrl || '';
    const afToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // ── Tree helpers ──────────────────────────────────────────────────────────
    function escH(s) {
        return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }
    function escJ(s) { return String(s).replace(/\\/g,'\\\\').replace(/'/g,"\\'"); }

    function buildNode(items, parentId) {
        return items
            .filter(i => i.parentId === parentId)
            .sort((a, b) => a.naam.localeCompare(b.naam, 'nl'))
            .map(i => ({ ...i, children: buildNode(items, i.id) }));
    }

    function canDelete(node) {
        return node.docCount === 0 && node.children.every(canDelete);
    }

    function renderNodes(nodes, depth) {
        if (!nodes.length) return '';
        let html = '<ul class="folder-list' + (depth > 0 ? ' folder-children' : '') + '">';
        for (const n of nodes) {
            const active = n.id === currentFolderId;
            const icon = active ? 'bi-folder2-open text-primary' : 'bi-folder';
            const deletable = canDelete(n);
            const hasChildren = n.children.length > 0;

            html += `<li>
              <div class="d-flex align-items-center folder-item">
                <button class="toggle-btn" onclick="toggleFolder(this)" aria-label="In/uitklappen">
                  <i class="bi ${hasChildren ? 'bi-chevron-down' : 'bi-dot'}"></i>
                </button>
                <a href="?projectId=${projectId}&folderId=${n.id}"
                   class="folder-link${active ? ' active' : ''}"
                   data-bs-toggle="tooltip" data-bs-placement="right" title="${escH(n.naam)}">
                  <i class="bi ${icon}"></i>
                  <span class="flex-grow-1 text-truncate">${escH(n.naam)}</span>
                  ${n.docCount > 0 ? `<span class="badge rounded-pill bg-light text-secondary" style="font-size:.7rem">${n.docCount}</span>` : ''}
                </a>
                <span class="folder-btns">
                  <button class="btn-fdr" onclick="openCreateModal(${n.id})" title="Submap aanmaken">
                    <i class="bi bi-folder-plus"></i>
                  </button>
                  <button class="btn-fdr" onclick="openRenameModal(${n.id}, '${escJ(n.naam)}')" title="Hernoemen">
                    <i class="bi bi-pencil"></i>
                  </button>
                  ${deletable
                    ? `<button class="btn-fdr text-danger" onclick="doDeleteFolder(${n.id})" title="Verwijderen"><i class="bi bi-trash"></i></button>`
                    : `<button class="btn-fdr disabled" disabled title="Map bevat inhoud"><i class="bi bi-trash"></i></button>`}
                </span>
              </div>
              ${hasChildren ? renderNodes(n.children, depth + 1) : ''}
            </li>`;
        }
        html += '</ul>';
        return html;
    }

    window.toggleFolder = function (btn) {
        const li = btn.closest('li');
        const children = li.querySelector('.folder-children');
        if (!children) return;
        const isHidden = children.style.display === 'none';
        children.style.display = isHidden ? '' : 'none';
        const icon = btn.querySelector('i');
        icon.className = isHidden ? 'bi bi-chevron-down' : 'bi bi-chevron-right';
    };

    function renderTree() {
        const tree = buildNode(folderData, null);
        const allActive = currentFolderId === null;
        let html = `<a href="?projectId=${projectId}" class="root-link${allActive ? ' active' : ''} text-dark">
          <i class="bi bi-house-door${allActive ? ' text-primary' : ''}"></i>
          <span class="flex-grow-1">Alle documenten</span>
          <span class="badge rounded-pill bg-light text-secondary" style="font-size:.7rem">${totalDocCount}</span>
        </a>`;
        html += renderNodes(tree, 0);
        html += `<div class="mt-2 pt-2 border-top">
          <button class="btn btn-sm btn-outline-secondary w-100" onclick="openCreateModal(null)">
            <i class="bi bi-folder-plus me-1"></i>Nieuwe map
          </button>
        </div>`;
        document.getElementById('folderTree').innerHTML = html;
    }

    renderTree();

    document.querySelectorAll('#folderTree [data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el, { trigger: 'hover' });
    });

    // ── Drag and drop ─────────────────────────────────────────────────────────
    let draggedBronId = null;

    document.querySelectorAll('.doc-row').forEach(row => {
        row.addEventListener('dragstart', e => {
            draggedBronId = row.dataset.bronId;
            row.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', draggedBronId);
        });
        row.addEventListener('dragend', () => {
            row.classList.remove('dragging');
        });
    });

    function makeDropTarget(el, folderId) {
        el.addEventListener('dragover', e => {
            if (!draggedBronId) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
        });
        el.addEventListener('dragenter', e => {
            if (!draggedBronId) return;
            e.preventDefault();
            el.classList.add('drop-target');
        });
        el.addEventListener('dragleave', e => {
            if (!el.contains(e.relatedTarget)) {
                el.classList.remove('drop-target');
            }
        });
        el.addEventListener('drop', async e => {
            e.preventDefault();
            el.classList.remove('drop-target');
            if (!draggedBronId) return;
            const params = { bronId: draggedBronId };
            if (folderId !== null) params.folderId = folderId;
            const data = await ajaxPost('/Chunk/MoveDocument', params);
            if (data.success) location.reload();
            else alert(data.message);
        });
    }

    function initDropTargets() {
        const rootLink = document.querySelector('.root-link');
        if (rootLink) makeDropTarget(rootLink, null);

        document.querySelectorAll('.folder-item').forEach(item => {
            const link = item.querySelector('.folder-link');
            if (!link) return;
            const match = (link.getAttribute('href') || '').match(/folderId=(\d+)/);
            if (!match) return;
            makeDropTarget(item, match[1]);
        });
    }

    initDropTargets();

    // ── Folder CRUD ───────────────────────────────────────────────────────────
    async function ajaxPost(url, params) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ __RequestVerificationToken: afToken, ...params })
        });
        return resp.json();
    }

    let createParentId = null;
    window.openCreateModal = function (parentId) {
        createParentId = parentId;
        document.getElementById('createFolderInput').value = '';
        bootstrap.Modal.getOrCreateInstance(document.getElementById('createFolderModal')).show();
        setTimeout(() => document.getElementById('createFolderInput').focus(), 300);
    };

    document.getElementById('createFolderForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const naam = document.getElementById('createFolderInput').value.trim();
        if (!naam) return;
        const params = { naam, projectId };
        if (createParentId !== null) params.parentId = createParentId;
        const data = await ajaxPost('/Chunk/FolderCreate', params);
        if (data.success) location.reload();
        else alert(data.message);
    });

    let renameFolderId = null;
    window.openRenameModal = function (id, naam) {
        renameFolderId = id;
        document.getElementById('renameFolderInput').value = naam;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('renameFolderModal')).show();
        setTimeout(() => document.getElementById('renameFolderInput').select(), 300);
    };

    document.getElementById('renameFolderForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const naam = document.getElementById('renameFolderInput').value.trim();
        if (!naam) return;
        const data = await ajaxPost('/Chunk/FolderRename', { id: renameFolderId, naam });
        if (data.success) location.reload();
        else alert(data.message);
    });

    window.doDeleteFolder = async function (id) {
        if (!confirm('Weet je zeker dat je deze map wilt verwijderen?')) return;
        const data = await ajaxPost('/Chunk/FolderDelete', { id });
        if (data.success) location.reload();
        else alert(data.message);
    };

    // ── Move document ─────────────────────────────────────────────────────────
    let moveBronId = null;

    function buildFolderOptions(nodes, select, depth) {
        for (const n of nodes) {
            const opt = document.createElement('option');
            opt.value = n.id;
            opt.textContent = '\u00a0\u00a0'.repeat(depth) + '\uD83D\uDCC1 ' + n.naam;
            select.appendChild(opt);
            buildFolderOptions(n.children, select, depth + 1);
        }
    }

    document.querySelectorAll('.btn-move').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            moveBronId = this.dataset.bronId;
            const sel = document.getElementById('moveFolderSelect');
            sel.innerHTML = '<option value="">\u2014 Geen map (onbestand) \u2014</option>';
            buildFolderOptions(buildNode(folderData, null), sel, 0);
            const cur = this.dataset.currentFolder;
            if (cur) sel.value = cur;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('moveModal')).show();
        });
    });

    document.getElementById('moveFolderForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const folderId = document.getElementById('moveFolderSelect').value;
        const params = { bronId: moveBronId };
        if (folderId) params.folderId = folderId;
        const data = await ajaxPost('/Chunk/MoveDocument', params);
        if (data.success) location.reload();
        else alert(data.message);
    });

    // ── Delete modal ──────────────────────────────────────────────────────────
    document.querySelectorAll('.btn-delete').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            document.getElementById('deleteFileName').textContent = this.dataset.bronName;
            document.getElementById('deleteId').value = this.dataset.bronId;
            document.getElementById('deleteForm').action = deleteActionBase + '/' + this.dataset.bronId;
        });
    });

    // ── Preview ───────────────────────────────────────────────────────────────
    document.querySelectorAll('.doc-row').forEach(row => {
        row.addEventListener('click', function (e) {
            if (e.target.closest('.doc-actions')) return;
            loadPreview(this.dataset.bronId, this.dataset.fileName);
        });
    });

    // ── Rename document ───────────────────────────────────────────────────────
    let renameDocBronId = null;

    document.querySelectorAll('.btn-rename-doc').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            renameDocBronId = this.dataset.bronId;
            document.getElementById('renameDocInput').value = this.dataset.currentTitle;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('renameDocModal')).show();
            setTimeout(() => document.getElementById('renameDocInput').select(), 300);
        });
    });

    document.getElementById('renameDocForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const title = document.getElementById('renameDocInput').value.trim();
        if (!title) return;
        const data = await ajaxPost('/Chunk/RenameDocument', { id: renameDocBronId, title });
        if (data.success) location.reload();
        else alert(data.message);
    });

    // ── Search / filter ───────────────────────────────────────────────────────
    const searchInput = document.getElementById('docSearch');
    const searchClear = document.getElementById('docSearchClear');

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const term = this.value.toLowerCase().trim();
            searchClear.classList.toggle('d-none', !term);

            document.querySelectorAll('.doc-row').forEach(row => {
                const title = (row.dataset.title || '').toLowerCase();
                const file = (row.dataset.fileName || '').toLowerCase();
                row.style.display = (!term || title.includes(term) || file.includes(term)) ? '' : 'none';
            });

            const visible = document.querySelectorAll('.doc-row:not([style*="display: none"])').length;
            let hint = document.getElementById('searchNoResults');
            if (!visible && term) {
                if (!hint) {
                    hint = document.createElement('tr');
                    hint.id = 'searchNoResults';
                    hint.innerHTML = '<td colspan="4" class="text-center text-muted py-3">Geen documenten gevonden voor "<span id="searchTerm"></span>"</td>';
                    document.querySelector('tbody').appendChild(hint);
                }
                hint.querySelector('#searchTerm').textContent = this.value;
            } else if (hint) {
                hint.remove();
            }
        });

        searchClear.addEventListener('click', function () {
            searchInput.value = '';
            searchInput.dispatchEvent(new Event('input'));
            searchInput.focus();
        });
    }

    // ── Set document link ──────────────────────────────────────────────────────
    let setLinkBronId = null;

    document.querySelectorAll('.btn-set-link').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            setLinkBronId = this.dataset.bronId;
            document.getElementById('setLinkInput').value = this.dataset.currentLink;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('setLinkModal')).show();
            setTimeout(() => document.getElementById('setLinkInput').focus(), 300);
        });
    });

    document.getElementById('setLinkForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        const link = document.getElementById('setLinkInput').value.trim();
        const data = await ajaxPost('/Chunk/SetDocumentLink', { id: setLinkBronId, link });
        if (data.success) location.reload();
        else alert(data.message);
    });

    function loadPreview(bronId, fileName) {
        const offcanvas = bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('previewPanel'));
        document.getElementById('previewTitle').textContent = fileName;
        document.getElementById('previewFileName').textContent = '';
        document.getElementById('previewContent').textContent = '';
        document.getElementById('previewContent').classList.add('d-none');
        document.getElementById('previewError').classList.add('d-none');
        document.getElementById('previewTruncatedNote').classList.add('d-none');
        document.getElementById('previewLoading').classList.remove('d-none');

        const ext = fileName.split('.').pop().toLowerCase();
        document.getElementById('previewIcon').className = ext === 'docx'
            ? 'bi bi-file-earmark-word text-primary me-2'
            : 'bi bi-file-earmark-text text-secondary me-2';

        offcanvas.show();

        fetch(previewUrl + '/' + bronId)
            .then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
            .then(data => {
                document.getElementById('previewLoading').classList.add('d-none');
                document.getElementById('previewContent').textContent = data.text || '(geen tekst gevonden)';
                document.getElementById('previewContent').classList.remove('d-none');
                if (data.truncated) document.getElementById('previewTruncatedNote').classList.remove('d-none');
            })
            .catch(err => {
                document.getElementById('previewLoading').classList.add('d-none');
                document.getElementById('previewError').textContent = 'Kon het document niet laden: ' + err.message;
                document.getElementById('previewError').classList.remove('d-none');
            });
    }
})();
