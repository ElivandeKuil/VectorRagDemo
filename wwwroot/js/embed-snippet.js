// embed-snippet.js — copy embed code and toggle embed key visibility
// Used by Chat/Settings.cshtml and Project/Edit.cshtml

function copyEmbedSnippet() {
    var input = document.getElementById('embedSnippet');
    navigator.clipboard.writeText(input.value).then(function () {
        var icon = document.getElementById('copyIcon');
        icon.className = 'bi bi-check2';
        setTimeout(function () { icon.className = 'bi bi-clipboard'; }, 2000);
    });
}

function toggleEmbedKey() {
    var field = document.getElementById('embedKeyField');
    var icon = document.getElementById('eyeIcon');
    if (field.type === 'password') {
        field.type = 'text';
        icon.className = 'bi bi-eye-slash';
    } else {
        field.type = 'password';
        icon.className = 'bi bi-eye';
    }
}
