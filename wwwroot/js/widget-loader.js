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
        console.warn('[ElAi Widget] No embed key provided in script src.');
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
        buttonShape: 'circle',
        buttonBorderWidth: 0,
        buttonBorderColor: '#ffffff',
        buttonIconPadding: 15,
        buttonLogoUrl: '',
        popupWidth: 380,
        popupHeight: 560,
        popupBorderRadius: 12,
        iconHideable: false,
        hideSide: 'right',
        peekAmount: 50,
        buttonSvgIdle: '',
        buttonSvgPeek: '',
        buttonSvgOpen: '',
        introMessage: '',
        introMessageDelay: 3
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

        var borderRadius = cfg.buttonShape === 'circle'  ? '50%'
                         : cfg.buttonShape === 'rounded' ? Math.round(cfg.buttonSize * 0.25) + 'px'
                         : '0';
        var border = cfg.buttonBorderWidth > 0
            ? cfg.buttonBorderWidth + 'px solid ' + cfg.buttonBorderColor
            : 'none';
        var iconMax = Math.max(8, cfg.buttonSize - 2 * cfg.buttonIconPadding);

        return [
            '#' + WIDGET_ID + '-btn {',
            '  position: fixed;',
            '  bottom: ' + cfg.offsetY + 'px;',
            '  ' + hEdge + ': ' + cfg.offsetX + 'px;',
            '  ' + hEdgeOpp + ': auto;',
            '  width: ' + cfg.buttonSize + 'px;',
            '  height: ' + cfg.buttonSize + 'px;',
            '  border-radius: ' + borderRadius + ';',
            '  background: ' + cfg.buttonColor + ';',
            '  color: #fff;',
            '  border: ' + border + ';',
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
            '  max-width: ' + iconMax + 'px;',
            '  max-height: ' + iconMax + 'px;',
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
            // Intro bubble
            '#' + WIDGET_ID + '-bubble {',
            '  position: fixed;',
            '  bottom: ' + (cfg.offsetY + cfg.buttonSize + 14) + 'px;',
            '  ' + hEdge + ': ' + cfg.offsetX + 'px;',
            '  ' + hEdgeOpp + ': auto;',
            '  background: #fff;',
            '  color: #212529;',
            '  border-radius: 10px;',
            '  padding: 10px 34px 10px 14px;',
            '  box-shadow: 0 4px 20px rgba(0,0,0,0.18);',
            '  font-size: 14px;',
            '  line-height: 1.45;',
            '  max-width: 240px;',
            '  z-index: 2147483644;',
            '  cursor: default;',
            '  animation: ' + WIDGET_ID + '-bubble-in 0.35s ease;',
            '}',
            '#' + WIDGET_ID + '-bubble::after {',
            '  content: "";',
            '  position: absolute;',
            '  bottom: -8px;',
            '  ' + hEdge + ': ' + Math.max(8, Math.round(cfg.buttonSize / 2) - 8) + 'px;',
            '  border: 8px solid transparent;',
            '  border-bottom: none;',
            '  border-top-color: #fff;',
            '}',
            '#' + WIDGET_ID + '-bubble-close {',
            '  position: absolute;',
            '  top: 6px;',
            '  right: 8px;',
            '  background: none;',
            '  border: none;',
            '  cursor: pointer;',
            '  font-size: 18px;',
            '  line-height: 1;',
            '  color: #adb5bd;',
            '  padding: 0 2px;',
            '}',
            '#' + WIDGET_ID + '-bubble-close:hover { color: #495057; }',
            '@keyframes ' + WIDGET_ID + '-bubble-in {',
            '  from { opacity: 0; transform: translateY(6px); }',
            '  to   { opacity: 1; transform: translateY(0); }',
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
            '  #' + WIDGET_ID + '-bubble {',
            '    max-width: calc(100vw - ' + (cfg.offsetX * 2 + 16) + 'px);',
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
            var imgSize = Math.max(8, c.buttonSize - 2 * c.buttonIconPadding) + 'px';
            return '<img src="' + c.buttonLogoUrl + '" width="' + imgSize + '" height="' + imgSize +
                   '" alt="" style="border-radius:50%;object-fit:cover;display:block;" />';
        }

        return defaultIconHtml(Math.max(8, c.buttonSize - 2 * c.buttonIconPadding));
    }

    /**
     * Compute the {x, y} translation so that only peekAmount% of the button
     * is visible at the chosen hide edge.
     */
    function computeHideTranslate(cfg) {
        var isLeft      = cfg.position === 'bottom-left';
        var hideFrac    = 1 - (cfg.peekAmount / 100);
        var partial     = Math.round(cfg.buttonSize * hideFrac);
        var visiblePart = cfg.buttonSize - partial;

        switch (cfg.hideSide) {
            case 'left':
                return {
                    x: isLeft ? -(cfg.offsetX + partial)
                              : -(window.innerWidth - cfg.offsetX - visiblePart),
                    y: 0
                };
            case 'right':
                return {
                    x: isLeft ? window.innerWidth - cfg.offsetX - visiblePart
                              : cfg.offsetX + partial,
                    y: 0
                };
            case 'top':
                return {
                    x: 0,
                    y: -(window.innerHeight - cfg.offsetY - visiblePart)
                };
            case 'bottom':
            default:
                return {
                    x: 0,
                    y: cfg.offsetY + partial
                };
        }
    }

    function init(cfg) {
        var c = {
            position:           cfg.widgetPosition    || defaults.position,
            offsetX:            cfg.offsetX           != null ? cfg.offsetX           : defaults.offsetX,
            offsetY:            cfg.offsetY           != null ? cfg.offsetY           : defaults.offsetY,
            buttonColor:        cfg.buttonColor       || defaults.buttonColor,
            buttonSize:         cfg.buttonSize        != null ? cfg.buttonSize        : defaults.buttonSize,
            buttonShape:        cfg.buttonShape       || defaults.buttonShape,
            buttonBorderWidth:  cfg.buttonBorderWidth != null ? cfg.buttonBorderWidth : defaults.buttonBorderWidth,
            buttonBorderColor:  cfg.buttonBorderColor || defaults.buttonBorderColor,
            buttonIconPadding:  cfg.buttonIconPadding != null ? cfg.buttonIconPadding : defaults.buttonIconPadding,
            buttonLogoUrl:      cfg.buttonLogoUrl     || defaults.buttonLogoUrl,
            popupWidth:         cfg.popupWidth        != null ? cfg.popupWidth        : defaults.popupWidth,
            popupHeight:        cfg.popupHeight       != null ? cfg.popupHeight       : defaults.popupHeight,
            popupBorderRadius:  cfg.popupBorderRadius != null ? cfg.popupBorderRadius : defaults.popupBorderRadius,
            iconHideable:       cfg.iconHideable      != null ? cfg.iconHideable      : defaults.iconHideable,
            hideSide:           cfg.hideSide          || defaults.hideSide,
            peekAmount:         cfg.peekAmount        != null ? cfg.peekAmount        : defaults.peekAmount,
            buttonSvgIdle:       cfg.buttonSvgIdle       || defaults.buttonSvgIdle,
            buttonSvgPeek:       cfg.buttonSvgPeek       || defaults.buttonSvgPeek,
            buttonSvgOpen:       cfg.buttonSvgOpen       || defaults.buttonSvgOpen,
            introMessage:        cfg.introMessage        || defaults.introMessage,
            introMessageDelay:   cfg.introMessageDelay   != null ? cfg.introMessageDelay : defaults.introMessageDelay,
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
        var hideTranslate = c.iconHideable ? computeHideTranslate(c) : { x: 0, y: 0 };
        var currentIconState = null;
        var peekTimer = null;
        var revealTimer = null; // lockout period after removePeek so the slide-out animation completes

        // ── Icon state ────────────────────────────────────────────────────────
        // Only replaces innerHTML when the state actually changes, so CSS
        // animations inside the SVG are never needlessly restarted.
        function setIcon(state) {
            if (state === currentIconState) return;
            currentIconState = state;
            btn.innerHTML = resolveIconHtml(state, c);
        }

        // ── Peek / hide behaviour ─────────────────────────────────────────────
        var bubbleVisible = false;

        function shouldPeek() {
            return c.iconHideable && !hasConversation && !isOpen && !bubbleVisible;
        }

        function applyPeek() {
            clearTimeout(peekTimer);
            peekTimer = null;
            clearTimeout(revealTimer);
            revealTimer = null;
            btn.classList.add('elai-btn-peeking');
            btn.style.transform = 'translate(' + hideTranslate.x + 'px,' + hideTranslate.y + 'px)';
            setIcon('peek');
        }

        function removePeek() {
            clearTimeout(peekTimer);
            peekTimer = null;
            btn.classList.remove('elai-btn-peeking');
            btn.style.transform = '';
            setIcon(isOpen ? 'open' : 'idle');
            // Prevent re-peeking until the slide-out CSS transition (300ms) finishes.
            // Without this the mousemove fires mid-slide, detects the cursor left the
            // peek zone, and schedules applyPeek before the button reaches its resting spot.
            clearTimeout(revealTimer);
            revealTimer = setTimeout(function () { revealTimer = null; }, 350);
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
                var margin  = 8;
                var isLeft  = c.position === 'bottom-left';

                if (c.hideSide === 'left' || c.hideSide === 'right') {
                    // Horizontal slide — constrain Y to the button's row, check the target X edge.
                    var rowTop = window.innerHeight - c.offsetY - c.buttonSize - margin;
                    var rowBtm = window.innerHeight - c.offsetY + margin;
                    if (e.clientY < rowTop || e.clientY > rowBtm) return false;
                    return c.hideSide === 'left'
                        ? e.clientX <= c.offsetX + c.buttonSize + margin
                        : e.clientX >= window.innerWidth - c.offsetX - c.buttonSize - margin;
                } else {
                    // Vertical slide (top/bottom) — constrain X to the button's column, check the target Y edge.
                    var colLeft  = isLeft ? c.offsetX - margin
                                          : window.innerWidth - c.offsetX - c.buttonSize - margin;
                    var colRight = isLeft ? c.offsetX + c.buttonSize + margin
                                          : window.innerWidth - c.offsetX + margin;
                    if (e.clientX < colLeft || e.clientX > colRight) return false;
                    return c.hideSide === 'top'
                        ? e.clientY <= c.buttonSize + margin
                        : e.clientY >= window.innerHeight - c.offsetY - c.buttonSize - margin;
                }
            }

            document.addEventListener('mousemove', function (e) {
                if (hasConversation || isOpen) return;
                if (isInPeekZone(e)) {
                    clearTimeout(peekTimer);
                    peekTimer = null;
                    if (btn.classList.contains('elai-btn-peeking')) removePeek();
                } else if (!btn.classList.contains('elai-btn-peeking') && !peekTimer && !revealTimer) {
                    peekTimer = setTimeout(function () {
                        peekTimer = null;
                        if (shouldPeek()) applyPeek();
                    }, 150);
                }
            });
        }
        // ─────────────────────────────────────────────────────────────────────

        // ── Intro bubble ──────────────────────────────────────────────────────
        var BUBBLE_SEEN_KEY = 'elai-intro-seen-' + key;

        function dismissBubble() {
            try { localStorage.setItem(BUBBLE_SEEN_KEY, '1'); } catch (_) {}
            bubbleVisible = false;
            var el = document.getElementById(WIDGET_ID + '-bubble');
            if (el) el.parentNode.removeChild(el);
            syncState(); // allow peek again if applicable
        }

        if (c.introMessage) {
            var alreadySeen = false;
            try { alreadySeen = !!localStorage.getItem(BUBBLE_SEEN_KEY); } catch (_) {}

            if (!alreadySeen) {
                setTimeout(function () {
                    bubbleVisible = true;
                    syncState(); // reveal button so bubble points to something visible

                    var bubble = document.createElement('div');
                    bubble.id = WIDGET_ID + '-bubble';

                    var text = document.createElement('span');
                    text.textContent = c.introMessage;
                    bubble.appendChild(text);

                    var closeBtn = document.createElement('button');
                    closeBtn.id = WIDGET_ID + '-bubble-close';
                    closeBtn.innerHTML = '&times;';
                    closeBtn.setAttribute('aria-label', 'Sluiten');
                    closeBtn.addEventListener('click', function (e) {
                        e.stopPropagation();
                        dismissBubble();
                    });
                    bubble.appendChild(closeBtn);

                    document.body.appendChild(bubble);
                }, Math.max(0, c.introMessageDelay) * 1000);
            }
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
            dismissBubble();
            if (isOpen) { closeWidget(); } else { openWidget(); }
        });

        window.addEventListener('message', function (event) {
            if (event.origin !== origin) return;
            if (event.data === 'elai-widget-close') closeWidget();
            if (event.data === 'elai-conversation-started') {
                dismissBubble();
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
