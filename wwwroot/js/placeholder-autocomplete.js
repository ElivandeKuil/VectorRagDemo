// placeholder-autocomplete.js — @@ autocomplete dropdown for textarea fields
// Used by Chunk/TextInput and Chunk/EditChunks views

class PlaceholderAutocomplete {
    constructor() {
        this.placeholders = [];
        this.activeTextarea = null;
        this.currentMatch = null;
        this.activeIndex = -1;
        this.dropdown = document.getElementById('phAutocomplete');
        document.addEventListener('click', e => {
            if (!this.dropdown.contains(e.target)) this.hide();
        });
    }

    updateList(list) { this.placeholders = list; }

    attach(ta) {
        ta.addEventListener('input', () => this.onInput(ta));
        ta.addEventListener('keydown', e => this.onKeyDown(e));
        ta.addEventListener('scroll', () => this.hide());
        ta.addEventListener('blur', () => setTimeout(() => this.hide(), 150));
    }

    onInput(ta) {
        this.activeTextarea = ta;
        const match = this.getAtMatch(ta);
        if (match) {
            const partial = match.partial.toLowerCase();
            const filtered = this.placeholders.filter(p => p.naam.toLowerCase().startsWith(partial));
            filtered.length > 0 ? this.show(ta, filtered, match) : this.hide();
        } else {
            this.hide();
        }
    }

    getAtMatch(ta) {
        const pos = ta.selectionStart;
        const before = ta.value.substring(0, pos);
        const m = before.match(/@@(\w*)$/);
        if (!m) return null;
        return { partial: m[1], start: pos - m[0].length };
    }

    show(ta, items, match) {
        this.currentMatch = match;
        this.activeIndex = 0;
        this.dropdown.innerHTML = items.map((p, i) =>
            `<div class="ph-ac-item${i === 0 ? ' active' : ''}" data-naam="${escapeHtml(p.naam)}" role="option">
                <span class="ph-ac-name">@@${escapeHtml(p.naam)}</span>
                <span class="ph-ac-value">${escapeHtml(p.waarde)}</span>
            </div>`
        ).join('');

        this.dropdown.querySelectorAll('.ph-ac-item').forEach((item, idx) => {
            item.addEventListener('mouseover', () => this.setActive(idx));
            item.addEventListener('mousedown', e => { e.preventDefault(); this.select(item.dataset.naam); });
        });

        const coords = this.getCaretCoords(ta);
        const rect = ta.getBoundingClientRect();
        this.dropdown.style.display = 'block';

        let top = rect.top + coords.top - ta.scrollTop + 20;
        let left = rect.left + coords.left;
        const ddRect = this.dropdown.getBoundingClientRect();
        if (top + 220 > window.innerHeight) top = rect.top + coords.top - ta.scrollTop - ddRect.height - 4;
        if (left + 220 > window.innerWidth) left = window.innerWidth - 225;
        if (left < 4) left = 4;
        this.dropdown.style.top = top + 'px';
        this.dropdown.style.left = left + 'px';
    }

    setActive(idx) {
        this.activeIndex = idx;
        this.dropdown.querySelectorAll('.ph-ac-item').forEach((item, i) => item.classList.toggle('active', i === idx));
    }

    onKeyDown(e) {
        if (this.dropdown.style.display === 'none') return;
        const items = this.dropdown.querySelectorAll('.ph-ac-item');
        if (!items.length) return;
        if (e.key === 'ArrowDown') { e.preventDefault(); this.setActive(Math.min(this.activeIndex + 1, items.length - 1)); }
        else if (e.key === 'ArrowUp') { e.preventDefault(); this.setActive(Math.max(this.activeIndex - 1, 0)); }
        else if (e.key === 'Enter' || e.key === 'Tab') { const a = items[this.activeIndex]; if (a) { e.preventDefault(); this.select(a.dataset.naam); } }
        else if (e.key === 'Escape') { e.preventDefault(); this.hide(); }
    }

    select(naam) {
        if (!this.activeTextarea) return;
        const match = this.getAtMatch(this.activeTextarea);
        if (!match) return;
        const ta = this.activeTextarea;
        const before = ta.value.substring(0, match.start);
        const after = ta.value.substring(ta.selectionStart);
        ta.value = before + '@@' + naam + after;
        const newPos = match.start + naam.length + 2;
        ta.setSelectionRange(newPos, newPos);
        ta.dispatchEvent(new Event('input'));
        this.hide();
        ta.focus();
    }

    hide() { this.dropdown.style.display = 'none'; this.currentMatch = null; this.activeIndex = -1; }

    getCaretCoords(ta) {
        const mirrorProps = [
            'borderTopWidth','borderRightWidth','borderBottomWidth','borderLeftWidth',
            'paddingTop','paddingRight','paddingBottom','paddingLeft',
            'fontFamily','fontSize','fontWeight','fontStyle','fontVariant',
            'letterSpacing','textTransform','wordSpacing','textIndent','lineHeight','boxSizing'
        ];
        const style = window.getComputedStyle(ta);
        const mirror = document.createElement('div');
        mirror.style.position = 'absolute';
        mirror.style.visibility = 'hidden';
        mirror.style.whiteSpace = 'pre-wrap';
        mirror.style.wordWrap = 'break-word';
        mirror.style.width = ta.clientWidth + 'px';
        mirrorProps.forEach(p => { mirror.style[p] = style[p]; });
        document.body.appendChild(mirror);
        mirror.textContent = ta.value.substring(0, ta.selectionStart);
        const span = document.createElement('span');
        span.textContent = '|';
        mirror.appendChild(span);
        const coords = { top: span.offsetTop, left: span.offsetLeft };
        document.body.removeChild(mirror);
        return coords;
    }
}

function escapeHtml(text) {
    return String(text).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
