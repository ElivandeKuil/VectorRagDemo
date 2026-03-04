(function () {
    'use strict';

    // Read the embed key and server origin from this script's own src attribute
    var scriptEl = document.currentScript ||
        (function () {
            var scripts = document.querySelectorAll('script[src*="widget-loader.js"]');
            return scripts[scripts.length - 1];
        })();

    if (!scriptEl) return;

    var scriptSrc = scriptEl.src;
    var scriptUrl = new URL(scriptSrc);
    var key = scriptUrl.searchParams.get('key');
    var origin = scriptUrl.origin; // e.g. https://elai.nl

    if (!key) {
        console.warn('[ElAI Widget] No embed key provided in script src.');
        return;
    }

    var WIDGET_ID = 'elai-widget-' + key.substring(0, 8);
    var IFRAME_URL = origin + '/Chat/Embed?key=' + encodeURIComponent(key);

    // Inject widget CSS
    var style = document.createElement('style');
    style.textContent = [
        '#' + WIDGET_ID + '-btn {',
        '  position: fixed;',
        '  bottom: 24px;',
        '  right: 24px;',
        '  width: 56px;',
        '  height: 56px;',
        '  border-radius: 50%;',
        '  background: #0d6efd;',
        '  color: #fff;',
        '  border: none;',
        '  cursor: pointer;',
        '  box-shadow: 0 4px 16px rgba(0,0,0,0.25);',
        '  display: flex;',
        '  align-items: center;',
        '  justify-content: center;',
        '  font-size: 24px;',
        '  z-index: 2147483646;',
        '  transition: transform 0.15s ease, box-shadow 0.15s ease;',
        '}',
        '#' + WIDGET_ID + '-btn:hover {',
        '  transform: scale(1.08);',
        '  box-shadow: 0 6px 20px rgba(0,0,0,0.32);',
        '}',
        '#' + WIDGET_ID + '-popup {',
        '  position: fixed;',
        '  bottom: 92px;',
        '  right: 24px;',
        '  width: 380px;',
        '  height: 560px;',
        '  border: none;',
        '  border-radius: 12px;',
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
        '    width: 100vw;',
        '    height: 70vh;',
        '    bottom: 0;',
        '    right: 0;',
        '    border-radius: 12px 12px 0 0;',
        '  }',
        '  #' + WIDGET_ID + '-btn {',
        '    bottom: 16px;',
        '    right: 16px;',
        '  }',
        '}',
    ].join('\n');
    document.head.appendChild(style);

    // Create the floating button
    var btn = document.createElement('button');
    btn.id = WIDGET_ID + '-btn';
    btn.setAttribute('aria-label', 'Open chat');
    btn.setAttribute('title', 'Open chat');
    btn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="26" height="26" fill="currentColor" viewBox="0 0 16 16"><path d="M2.678 11.894a1 1 0 0 1 .287.801 11 11 0 0 1-.398 2c1.395-.323 2.247-.697 2.634-.893a1 1 0 0 1 .71-.074A8 8 0 0 0 8 14c3.996 0 7-2.807 7-6s-3.004-6-7-6-7 2.808-7 6c0 1.468.617 2.83 1.678 3.894m-.493 3.905a22 22 0 0 1-.713.129c-.2.032-.352-.176-.273-.362a10 10 0 0 0 .244-.637l.003-.01c.248-.72.45-1.548.524-2.319C.743 11.37 0 9.76 0 8c0-3.866 3.582-7 8-7s8 3.134 8 7-3.582 7-8 7a9 9 0 0 1-2.088-.243 22 22 0 0 1-.713-.129Z"/></svg>';
    document.body.appendChild(btn);

    // Create the iframe popup
    var iframe = document.createElement('iframe');
    iframe.id = WIDGET_ID + '-popup';
    iframe.src = IFRAME_URL;
    iframe.allow = 'microphone';
    iframe.setAttribute('loading', 'lazy');
    document.body.appendChild(iframe);

    var isOpen = false;

    function openWidget() {
        iframe.style.display = 'block';
        // Force reflow so the CSS transition fires
        iframe.getBoundingClientRect();
        iframe.classList.add('elai-open');
        btn.setAttribute('aria-label', 'Close chat');
        isOpen = true;
    }

    function closeWidget() {
        iframe.classList.remove('elai-open');
        btn.setAttribute('aria-label', 'Open chat');
        isOpen = false;
        // Hide after transition
        setTimeout(function () {
            if (!isOpen) iframe.style.display = 'none';
        }, 220);
    }

    btn.addEventListener('click', function () {
        if (isOpen) {
            closeWidget();
        } else {
            openWidget();
        }
    });

    // Allow the iframe to close the popup by posting a message
    window.addEventListener('message', function (event) {
        if (event.origin !== origin) return;
        if (event.data === 'elai-widget-close') {
            closeWidget();
        }
    });
})();
