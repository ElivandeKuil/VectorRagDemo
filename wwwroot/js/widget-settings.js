// widget-settings.js — live preview for the widget settings page

var PREVIEW_W = document.getElementById('prev_popup')?.closest('.card-body')?.offsetWidth || 500;
var PREVIEW_H = document.getElementById('prev_popup')?.closest('.card-body')?.offsetHeight || 560;

function get(id) { return document.getElementById(id); }
function val(id) { return (get(id) || {}).value; }

function syncColorFromHex(pickerId, hexId) {
    var hex = val(hexId);
    if (/^#[0-9a-fA-F]{6}$/.test(hex)) get(pickerId).value = hex;
}

function fontSizePx(fs) {
    return fs === 'sm' ? '12px' : fs === 'lg' ? '15px' : '13px';
}

function updatePreview() {
    var position        = val('cfg_position');
    var offsetX         = parseInt(val('cfg_offsetx')) || 24;
    var offsetY         = parseInt(val('cfg_offsety')) || 24;
    var btnColor        = val('cfg_btncolor');
    var btnSize         = parseInt(val('cfg_btnsize')) || 56;
    var btnShape        = val('cfg_btnshape') || 'circle';
    var btnBorderWidth  = parseInt(val('cfg_btnborderwidth')) || 0;
    var btnBorderColor  = val('cfg_btnbordercolor') || '#ffffff';
    var btnIconPadding  = parseInt(val('cfg_iconpadding')) || 15;
    var btnLogo         = val('cfg_btnlogo') || '';
    var popupW      = parseInt(val('cfg_popupwidth')) || 380;
    var popupH      = parseInt(val('cfg_popupheight')) || 560;
    var borderR     = parseInt(val('cfg_borderradius')) || 12;
    var headerBg    = val('cfg_headerbg');
    var headerTxt   = val('cfg_headertxt');
    var headerTitle = val('cfg_headertitle') || '';
    var headerLogo  = val('cfg_headerlogo') || '';
    var chatBodyBg  = val('cfg_chatbodybg') || '#ffffff';
    var inputBg     = val('cfg_inputbg') || '#ffffff';
    var sendBtnBg   = val('cfg_sendbtnbg') || '#0d6efd';
    var sendBtnTxt  = val('cfg_sendbtntxt') || '#ffffff';
    var userBg      = val('cfg_userbg');
    var userTxt     = val('cfg_usertxt');
    var botBg       = val('cfg_botbg');
    var botTxt      = val('cfg_bottxt');
    var fontSize    = val('cfg_fontsize');
    var greeting    = val('cfg_greeting') || 'Hallo!';

    var container = get('prev_popup').parentElement;
    var cW = container.offsetWidth;
    var cH = container.offsetHeight;

    // Scale popup to fit preview area with padding
    var scaleW = Math.min(1, (cW - offsetX - 16) / popupW);
    var scaleH = Math.min(1, (cH - btnSize - offsetY - 16) / popupH);
    var scale  = Math.min(scaleW, scaleH, 0.85);
    var scaledW = Math.round(popupW * scale);
    var scaledH = Math.round(popupH * scale);
    var scaledBtn = Math.round(btnSize * scale);
    var scaledOffX = Math.round(offsetX * scale);
    var scaledOffY = Math.round(offsetY * scale);

    var popup = get('prev_popup');
    popup.style.width  = scaledW + 'px';
    popup.style.height = scaledH + 'px';
    popup.style.borderRadius = Math.round(borderR * scale) + 'px';
    if (position === 'bottom-left') {
        popup.style.left  = scaledOffX + 'px';
        popup.style.right = 'auto';
    } else {
        popup.style.right = scaledOffX + 'px';
        popup.style.left  = 'auto';
    }
    popup.style.bottom = (scaledBtn + scaledOffY + Math.round(8 * scale)) + 'px';

    var header = get('prev_header');
    header.style.background = headerBg;
    header.style.color      = headerTxt;

    // Header title
    get('prev_botname').textContent = headerTitle || 'Bot';
    get('prev_botname').style.color = headerTxt;

    // Header logo vs default icon
    var logoEl = get('prev_headerlogo');
    var iconEl = get('prev_defaulticon');
    if (headerLogo) {
        logoEl.src = headerLogo;
        logoEl.style.display = 'inline-block';
        iconEl.style.display = 'none';
    } else {
        logoEl.style.display = 'none';
        iconEl.style.display = 'inline-block';
    }

    // Chat body background
    get('prev_messages').style.background = chatBodyBg;

    // Input area
    get('prev_inputbar').style.background = inputBg;

    // Sync hex inputs with color pickers
    ['btncolor','btnbordercolor','headerbg','headertxt','chatbodybg','inputbg','sendbtnbg','sendbtntxt','userbg','usertxt','botbg','bottxt'].forEach(function(k) {
        var picker = get('cfg_' + k);
        var hexBox = get('cfg_' + k + '_hex');
        if (picker && hexBox && document.activeElement !== hexBox) hexBox.value = picker.value;
    });

    // Bubbles
    var fsPx = fontSizePx(fontSize);
    get('prev_greeting').textContent = greeting;
    get('prev_greeting').style.cssText += '; background:' + botBg + '; color:' + botTxt + '; font-size:' + fsPx;
    get('prev_usermsg').style.cssText  += '; background:' + userBg + '; color:' + userTxt + '; font-size:' + fsPx;
    get('prev_botmsg').style.cssText   += '; background:' + botBg + '; color:' + botTxt + '; font-size:' + fsPx;

    // Send button
    var sendBtn = get('prev_sendbtn');
    sendBtn.style.background = sendBtnBg;
    sendBtn.style.color      = sendBtnTxt;
    sendBtn.querySelector('svg').setAttribute('fill', sendBtnTxt);

    // Floating button
    var btn = get('prev_btn');
    btn.style.width      = scaledBtn + 'px';
    btn.style.height     = scaledBtn + 'px';
    btn.style.background = btnLogo ? 'transparent' : btnColor;
    var scaledBorderW = Math.round(btnBorderWidth * scale);
    btn.style.border = scaledBorderW > 0 ? scaledBorderW + 'px solid ' + btnBorderColor : 'none';
    var btnBr = btnShape === 'circle'  ? '50%'
              : btnShape === 'rounded' ? Math.round(scaledBtn * 0.25) + 'px'
              : '0';
    btn.style.borderRadius = btnBr;
    if (position === 'bottom-left') {
        btn.style.left  = scaledOffX + 'px';
        btn.style.right = 'auto';
    } else {
        btn.style.right = scaledOffX + 'px';
        btn.style.left  = 'auto';
    }
    btn.style.bottom = scaledOffY + 'px';

    // Button icon — SVG textarea → logo URL → default bubble
    var btnLogoEl      = get('prev_btnlogo');
    var btnIconEl      = get('prev_btnicon');
    var btnCustomSvgEl = get('prev_btncustomsvg');
    var iconMax        = Math.max(8, scaledBtn - 2 * Math.round(btnIconPadding * scale));

    var svgIdle = (val('cfg_svgidle') || '').trim();
    var svgPeek = (val('cfg_svgpeek') || '').trim();
    var iconHideableNow = get('cfg_iconhideable') && get('cfg_iconhideable').checked;
    var isPeeking = iconHideableNow && !_previewRevealed;
    var activeSvg = isPeeking ? (svgPeek || svgIdle) : svgIdle;

    if (activeSvg) {
        btnCustomSvgEl.innerHTML = activeSvg;
        var innerSvg = btnCustomSvgEl.querySelector('svg');
        if (innerSvg) {
            innerSvg.style.maxWidth  = iconMax + 'px';
            innerSvg.style.maxHeight = iconMax + 'px';
            innerSvg.style.width     = 'auto';
            innerSvg.style.height    = 'auto';
            innerSvg.style.display   = 'block';
        }
        btnCustomSvgEl.style.display = 'block';
        btnLogoEl.style.display = 'none';
        btnIconEl.style.display = 'none';
    } else if (btnLogo) {
        btnCustomSvgEl.style.display = 'none';
        btnLogoEl.src = btnLogo;
        btnLogoEl.style.width   = iconMax + 'px';
        btnLogoEl.style.height  = iconMax + 'px';
        btnLogoEl.style.display = 'block';
        btnIconEl.style.display = 'none';
    } else {
        btnCustomSvgEl.style.display = 'none';
        btnLogoEl.style.display = 'none';
        btnIconEl.style.display = 'block';
        btnIconEl.setAttribute('width', iconMax);
        btnIconEl.setAttribute('height', iconMax);
    }

    // Intro bubble
    var introMsg    = (val('cfg_intromessage') || '').trim();
    var bubble      = get('prev_bubble');
    var bubbleText  = get('prev_bubbletext');
    var tailEl      = bubble ? bubble.querySelector('div') : null;
    if (introMsg && bubble) {
        bubbleText.textContent = introMsg;
        var bubbleBottom = scaledOffY + scaledBtn + Math.round(10 * scale);
        bubble.style.bottom = bubbleBottom + 'px';
        if (position === 'bottom-left') {
            bubble.style.left  = scaledOffX + 'px';
            bubble.style.right = 'auto';
            if (tailEl) { tailEl.style.left = Math.max(6, Math.round(scaledBtn / 2) - 6) + 'px'; tailEl.style.right = 'auto'; }
        } else {
            bubble.style.right = scaledOffX + 'px';
            bubble.style.left  = 'auto';
            if (tailEl) { tailEl.style.right = Math.max(6, Math.round(scaledBtn / 2) - 6) + 'px'; tailEl.style.left = 'auto'; }
        }
        bubble.style.display = 'block';
    } else if (bubble) {
        bubble.style.display = 'none';
    }

    // Peek offset
    var hideSide   = val('cfg_hideside') || 'right';
    var peekAmount = parseInt(val('cfg_peekamount')) || 50;
    if (iconHideableNow && !_previewRevealed) {
        var hideFrac    = 1 - (peekAmount / 100);
        var partial     = Math.round(scaledBtn * hideFrac);
        var visiblePart = scaledBtn - partial;
        var isLeftPos   = position === 'bottom-left';
        var peekTX = 0, peekTY = 0;
        switch (hideSide) {
            case 'left':
                peekTX = isLeftPos ? -(scaledOffX + partial) : -(cW - scaledOffX - visiblePart);
                break;
            case 'right':
                peekTX = isLeftPos ? cW - scaledOffX - visiblePart : scaledOffX + partial;
                break;
            case 'top':
                peekTY = -(cH - scaledOffY - visiblePart);
                break;
            case 'bottom':
                peekTY = scaledOffY + partial;
                break;
        }
        btn.style.transform  = 'translate(' + peekTX + 'px,' + peekTY + 'px)';
        btn.style.transition = 'transform 0.3s ease';
    } else {
        btn.style.transform  = '';
        btn.style.transition = 'transform 0.3s ease';
    }
}

// Interactive peek in the preview
var _previewRevealed = false;
var _previewPeekTimer = null;
var _previewRevealTimer = null;

function previewReveal() {
    clearTimeout(_previewPeekTimer);
    _previewPeekTimer = null;
    clearTimeout(_previewRevealTimer);
    _previewRevealTimer = setTimeout(function () { _previewRevealTimer = null; }, 350);
    _previewRevealed = true;
    updatePreview();
}

function previewPeekBack() {
    if (_previewRevealTimer) return;
    clearTimeout(_previewPeekTimer);
    _previewPeekTimer = setTimeout(function () {
        _previewPeekTimer = null;
        _previewRevealed = false;
        updatePreview();
    }, 300);
}

document.addEventListener('DOMContentLoaded', function () {
    var posEl = get('cfg_position');
    if (posEl) posEl.addEventListener('change', updatePreview);
    var fsEl = get('cfg_fontsize');
    if (fsEl) fsEl.addEventListener('change', updatePreview);

    updatePreview();

    var container = get('prev_popup') && get('prev_popup').parentElement;
    if (container) {
        container.addEventListener('mousemove', function (e) {
            var iconHideable = get('cfg_iconhideable') && get('cfg_iconhideable').checked;
            if (!iconHideable) return;

            var rect        = container.getBoundingClientRect();
            var mx          = e.clientX - rect.left;
            var my          = e.clientY - rect.top;
            var hideSide    = val('cfg_hideside') || 'right';
            var hoverMargin = 56;
            var inZone;
            switch (hideSide) {
                case 'left':   inZone = mx <= hoverMargin; break;
                case 'top':    inZone = my <= hoverMargin; break;
                case 'bottom': inZone = my >= rect.height - hoverMargin; break;
                default:       inZone = mx >= rect.width - hoverMargin; break;
            }

            if (inZone) {
                if (!_previewRevealed) previewReveal();
                else { clearTimeout(_previewPeekTimer); _previewPeekTimer = null; }
            } else if (_previewRevealed && !_previewRevealTimer) {
                previewPeekBack();
            }
        });

        container.addEventListener('mouseleave', function () {
            var iconHideable = get('cfg_iconhideable') && get('cfg_iconhideable').checked;
            if (iconHideable && _previewRevealed) previewPeekBack();
        });
    }
});

window.addEventListener('resize', updatePreview);
