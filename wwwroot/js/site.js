// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Global function to load partial views
function loadPartialView(url, targetElementId, data = null, method = 'GET') {
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json'
        }
    };

    if (data && method === 'POST') {
        options.body = JSON.stringify(data);
    }

    return fetch(url, options)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(html => {
            const targetElement = document.getElementById(targetElementId);
            if (targetElement) {
                targetElement.innerHTML = html;
            }
            return html;
        })
        .catch(error => {
            console.error('Error loading partial view:', error);
            throw error;
        });
}
