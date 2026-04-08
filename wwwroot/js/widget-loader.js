(function () {
    'use strict';

    var scriptEl = document.currentScript ||
        (function () {
            var scripts = document.querySelectorAll('script[src*="widget-loader.js"]');
            return scripts[scripts.length - 1];
        })();

    if (!scriptEl) return;

    var key = scriptEl.dataset.key ||
              new URL(scriptEl.src, window.location.href).searchParams.get('key');
    var origin = window.location.origin;

    if (!key) {
        console.warn('[ElAI Widget] No embed key provided in script src.');
        return;
    }

    var WIDGET_ID = 'elai-widget-' + key.substring(0, 8);
    var IFRAME_URL = origin + '/Chat/Embed?key=' + encodeURIComponent(key);
    var CONFIG_URL = origin + '/Chat/GetWidgetConfig?key=' + encodeURIComponent(key);

    var defaults = {
        position: 'bottom-right',
        offsetX: 24,
        offsetY: 24,
        buttonColor: '#0d6efd',
        buttonSize: 56,
        buttonLogoUrl: '',
        popupWidth: 380,
        popupHeight: 560,
        popupBorderRadius: 12,
        iconHideable: false,
        hideSide: 'right',
        peekAmount: 50,
        buttonSvgIdle: '',
        buttonSvgPeek: '',
        buttonSvgOpen: ''
    };

    // Default chat-bubble SVG used when no custom icon is configured
    function defaultIconHtml(size) {
        return '<svg xmlns="http://www.w3.org/2000/svg" width="' + size + '" height="' + size + '" fill="currentColor" viewBox="0 0 16 16">' +
            '<path d="M2.678 11.894a1 1 0 0 1 .287.801 11 11 0 0 1-.398 2c1.395-.323 2.247-.697 2.634-.893a1 1 0 0 1 .71-.074A8 8 0 0 0 8 14c3.996 0 7-2.807 7-6s-3.004-6-7-6-7 2.808-7 6c0 1.468.617 2.83 1.678 3.894m-.493 3.905a22 22 0 0 1-.713.129c-.2.032-.352-.176-.273-.362a10 10 0 0 0 .244-.637l.003-.01c.248-.72.45-1.548.524-2.319C.743 11.37 0 9.76 0 8c0-3.866 3.582-7 8-7s8 3.134 8 7-3.582 7-8 7a9 9 0 0 1-2.088-.243 22 22 0 0 1-.713-.129Z"/>' +
            '</svg>';
    }

    function buildCss(cfg) {
        var isLeft   = cfg.position === 'bottom-left';
        var hEdge    = isLeft ? 'left' : 'right';
        var hEdgeOpp = isLeft ? 'right' : 'left';

        return [
            '#' + WIDGET_ID + '-btn {',
            '  position: fixed;',
            '  bottom: ' + cfg.offsetY + 'px;',
            '  ' + hEdge + ': ' + cfg.offsetX + 'px;',
            '  ' + hEdgeOpp + ': auto;',
            '  width: ' + cfg.buttonSize + 'px;',
            '  height: ' + cfg.buttonSize + 'px;',
            '  border-radius: 50%;',
            '  background: ' + cfg.buttonColor + ';',
            '  color: #fff;',
            '  border: none;',
            '  cursor: pointer;',
            '  box-shadow: 0 4px 16px rgba(0,0,0,0.25);',
            '  display: flex;',
            '  align-items: center;',
            '  justify-content: center;',
            '  font-size: 24px;',
            '  z-index: 2147483646;',
            '  overflow: hidden;',
            '  padding: 0;',
            '  transition: transform 0.3s ease, box-shadow 0.15s ease;',
            '}',
            '#' + WIDGET_ID + '-btn:not(.elai-btn-peeking):hover {',
            '  transform: scale(1.08);',
            '  box-shadow: 0 6px 20px rgba(0,0,0,0.32);',
            '}',
            // Ensure SVG/img inside the button fills it proportionally
            '#' + WIDGET_ID + '-btn svg,',
            '#' + WIDGET_ID + '-btn img {',
            '  display: block;',
            '  max-width: ' + Math.round(cfg.buttonSize * 0.62) + 'px;',
            '  max-height: ' + Math.round(cfg.buttonSize * 0.62) + 'px;',
            '  width: auto;',
            '  height: auto;',
            '}',
            '#' + WIDGET_ID + '-popup {',
            '  position: fixed;',
            '  bottom: ' + (cfg.offsetY + cfg.buttonSize + 8) + 'px;',
            '  ' + hEdge + ': ' + cfg.offsetX + 'px;',
            '  ' + hEdgeOpp + ': auto;',
            '  width: ' + cfg.popupWidth + 'px;',
            '  height: ' + cfg.popupHeight + 'px;',
            '  border: none;',
            '  border-radius: ' + cfg.popupBorderRadius + 'px;',
            '  box-shadow: 0 8px 32px rgba(0,0,0,0.22);',
            '  z-index: 2147483645;',
            '  overflow: hidden;',
            '  display: none;',
            '  transition: opacity 0.2s ease, transform 0.2s ease;',
            '  opacity: 0;',
            '  transform: translateY(12px);',
            '}',
            '#' + WIDGET_ID + '-popup.elai-open {',
            '  opacity: 1;',
            '  transform: translateY(0);',
            '}',
            '@media (max-width: 480px) {',
            '  #' + WIDGET_ID + '-popup {',
            '    width: 100vw !important;',
            '    height: 70vh !important;',
            '    bottom: 0 !important;',
            '    left: 0 !important;',
            '    right: 0 !important;',
            '    border-radius: 12px 12px 0 0 !important;',
            '  }',
            '  #' + WIDGET_ID + '-btn {',
            '    bottom: 16px;',
            '    ' + hEdge + ': 16px;',
            '  }',
            '}',
        ].join('\n');
    }

    /**
     * Returns the button innerHTML for the given state ('idle' | 'peek' | 'open').
     * Priority: state-specific SVG → idle SVG → ButtonLogoUrl image → default bubble.
     */
    function resolveIconHtml(state, c) {
        var svg = (state === 'peek' && c.buttonSvgPeek) ? c.buttonSvgPeek
                : (state === 'open' && c.buttonSvgOpen) ? c.buttonSvgOpen
                : c.buttonSvgIdle;

        if (svg) return svg;

        if (c.buttonLogoUrl) {
            var imgSize = Math.round(c.buttonSize * 0.62) + 'px';
            return '<img src="' + c.buttonLogoUrl + '" width="' + imgSize + '" height="' + imgSize +
                   '" alt="" style="border-radius:50%;object-fit:cover;display:block;" />';
        }

        return defaultIconHtml(Math.round(c.buttonSize * 0.46));
    }

    /**
     * Compute the X translation so that only 30% of the button is visible
     * at the given hide edge.
     */
    function computeHideTranslate(cfg) {
        var isLeft      = cfg.position === 'bottom-left';
        var hideLeft    = cfg.hideSide === 'left';
        var hideFrac    = 1 - (cfg.peekAmount / 100);   // hidden fraction, e.g. 0.5 for 50 % visible
        var partial     = Math.round(cfg.buttonSize * hideFrac);
        var visiblePart = cfg.buttonSize - partial;

        if (hideLeft) {
            return isLeft
                ? -(cfg.offsetX + partial)
                : -(window.innerWidth - cfg.offsetX - visiblePart);
        } else {
            return isLeft
                ? window.innerWidth - cfg.offsetX - visiblePart
                : cfg.offsetX + partial;
        }
    }

    function init(cfg) {
        var c = {
            position:          cfg.widgetPosition    || defaults.position,
            offsetX:           cfg.offsetX           != null ? cfg.offsetX           : defaults.offsetX,
            offsetY:           cfg.offsetY           != null ? cfg.offsetY           : defaults.offsetY,
            buttonColor:       cfg.buttonColor       || defaults.buttonColor,
            buttonSize:        cfg.buttonSize        != null ? cfg.buttonSize        : defaults.buttonSize,
            buttonLogoUrl:     cfg.buttonLogoUrl     || defaults.buttonLogoUrl,
            popupWidth:        cfg.popupWidth        != null ? cfg.popupWidth        : defaults.popupWidth,
            popupHeight:       cfg.popupHeight       != null ? cfg.popupHeight       : defaults.popupHeight,
            popupBorderRadius: cfg.popupBorderRadius != null ? cfg.popupBorderRadius : defaults.popupBorderRadius,
            iconHideable:      cfg.iconHideable      != null ? cfg.iconHideable      : defaults.iconHideable,
            hideSide:          cfg.hideSide          || defaults.hideSide,
            peekAmount:        cfg.peekAmount        != null ? cfg.peekAmount        : defaults.peekAmount,
            buttonSvgIdle:     cfg.buttonSvgIdle     || defaults.buttonSvgIdle,
            buttonSvgPeek:     cfg.buttonSvgPeek     || defaults.buttonSvgPeek,
            buttonSvgOpen:     cfg.buttonSvgOpen     || defaults.buttonSvgOpen,
        };

        // Inject CSS
        var style = document.createElement('style');
        style.textContent = buildCss(c);
        document.head.appendChild(style);

        // Floating button
        var btn = document.createElement('button');
        btn.id = WIDGET_ID + '-btn';
        btn.setAttribute('aria-label', 'Open chat');
        btn.setAttribute('title', 'Open chat');
        btn.innerHTML = resolveIconHtml('idle', c);
        document.body.appendChild(btn);

        // Iframe popup
        var iframe = document.createElement('iframe');
        iframe.id = WIDGET_ID + '-popup';
        iframe.src = IFRAME_URL;
        iframe.allow = 'microphone';
        iframe.setAttribute('loading', 'lazy');
        document.body.appendChild(iframe);

        var isOpen = false;
        var hasConversation = false;
        var hideTranslate = c.iconHideable ? computeHideTranslate(c) : 0;
        var currentIconState = null;
        var peekTimer = null;

        // ── Icon state ────────────────────────────────────────────────────────
        // Only replaces innerHTML when the state actually changes, so CSS
        // animations inside the SVG are never needlessly restarted.
        function setIcon(state) {
            if (state === currentIconState) return;
            currentIconState = state;
            btn.innerHTML = resolveIconHtml(state, c);
        }

        // ── Peek / hide behaviour ─────────────────────────────────────────────
        function shouldPeek() {
            return c.iconHideable && !hasConversation && !isOpen;
        }

        function applyPeek() {
            clearTimeout(peekTimer);
            peekTimer = null;
            btn.classList.add('elai-btn-peeking');
            btn.style.transform = 'translateX(' + hideTranslate + 'px)';
            setIcon('peek');
        }

        function removePeek() {
            clearTimeout(peekTimer);
            peekTimer = null;
            btn.classList.remove('elai-btn-peeking');
            btn.style.transform = '';
            setIcon(isOpen ? 'open' : 'idle');
        }

        function syncState() {
            if (shouldPeek()) { applyPeek(); } else { removePeek(); }
        }

        // Initial state — short delay so the peek slide-in is visible on load
        if (c.iconHideable) {
            setTimeout(function () { syncState(); }, 120);

            // Track mouse against a FIXED zone (the full path the button travels),
            // not against the button element itself which moves and causes the
            // cursor to fall outside it mid-slide, re-triggering the animation.
            function isInPeekZone(e) {
                var margin   = 8;
                var zoneTop  = window.innerHeight - c.offsetY - c.buttonSize - margin;
                var zoneBtm  = window.innerHeight - c.offsetY + margin;
                if (e.clientY < zoneTop || e.clientY > zoneBtm) return false;
                if (c.hideSide === 'left') {
                    return e.clientX <= c.offsetX + c.buttonSize + margin;
                } else {
                    return e.clientX >= window.innerWidth - c.offsetX - c.buttonSize - margin;
                }
            }

            document.addEventListener('mousemove', function (e) {
                if (hasConversation || isOpen) return;
                if (isInPeekZone(e)) {
                    clearTimeout(peekTimer);
                    peekTimer = null;
                    if (btn.classList.contains('elai-btn-peeking')) removePeek();
                } else if (!btn.classList.contains('elai-btn-peeking') && !peekTimer) {
                    peekTimer = setTimeout(function () {
                        peekTimer = null;
                        if (shouldPeek()) applyPeek();
                    }, 150);
                }
            });
        }
        // ─────────────────────────────────────────────────────────────────────

        function openWidget() {
            iframe.style.display = 'block';
            iframe.getBoundingClientRect(); // force reflow
            iframe.classList.add('elai-open');
            btn.setAttribute('aria-label', 'Close chat');
            isOpen = true;
            syncState();
        }

        function closeWidget() {
            iframe.classList.remove('elai-open');
            btn.setAttribute('aria-label', 'Open chat');
            isOpen = false;
            setTimeout(function () {
                if (!isOpen) iframe.style.display = 'none';
            }, 220);
            syncState();
        }

        btn.addEventListener('click', function () {
            if (isOpen) { closeWidget(); } else { openWidget(); }
        });

        window.addEventListener('message', function (event) {
            if (event.origin !== origin) return;
            if (event.data === 'elai-widget-close') closeWidget();
            if (event.data === 'elai-conversation-started') {
                hasConversation = true;
                syncState();
            }
        });
    }

    fetch(CONFIG_URL)
        .then(function (r) { return r.ok ? r.json() : {}; })
        .then(function (cfg) { init(cfg || {}); })
        .catch(function () { init({}); });
})();
