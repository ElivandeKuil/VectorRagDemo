// hoe-het-werkt.js — mode toggle for the "Hoe het werkt" marketing page

function switchMode(mode) {
    var self = document.getElementById('mode-self');
    var concierge = document.getElementById('mode-concierge');
    var btnSelf = document.getElementById('btn-self');
    var btnConcierge = document.getElementById('btn-concierge');

    if (mode === 'self') {
        self.style.display = '';
        concierge.style.display = 'none';
        btnSelf.classList.add('active');
        btnConcierge.classList.remove('active');
    } else {
        self.style.display = 'none';
        concierge.style.display = '';
        btnSelf.classList.remove('active');
        btnConcierge.classList.add('active');
    }
}
