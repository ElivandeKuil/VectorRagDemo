document.querySelectorAll('textarea').forEach(function (ta) {
    ta.style.boxSizing = 'border-box';
    ta.style.overflow = 'hidden';
    ta.style.resize = 'vertical';
    ta.style.minHeight = ta.offsetHeight + 'px';
    ta.style.height = '0';
    ta.style.height = ta.scrollHeight + 'px';
    ta.addEventListener('input', function () {
        this.style.height = '0';
        this.style.height = this.scrollHeight + 'px';
    });
});
