// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Helper function to parse markdown content in elements
function parseMarkdownContent(container) {
    const markdownElements = container.querySelectorAll('.markdown-content[data-markdown]');
    markdownElements.forEach(element => {
        try {
            const markdownAttr = element.getAttribute('data-markdown');

            // Skip if attribute is empty or whitespace
            if (!markdownAttr || !markdownAttr.trim()) {
                element.textContent = '';
                element.removeAttribute('data-markdown');
                return;
            }

            const markdownText = JSON.parse(markdownAttr);

            // Handle null or empty content
            if (!markdownText) {
                element.textContent = '';
                element.removeAttribute('data-markdown');
                return;
            }

            if (typeof marked !== 'undefined') {
                element.innerHTML = marked.parse(markdownText);
            } else {
                element.textContent = markdownText;
            }
            // Remove the data attribute after processing
            element.removeAttribute('data-markdown');
        } catch (e) {
            console.error('Error parsing markdown:', e, 'Attribute value:', element.getAttribute('data-markdown'));
            // Fallback: try to display the raw attribute value
            const rawValue = element.getAttribute('data-markdown');
            element.textContent = rawValue || '[Error displaying content]';
        }
    });
}

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
                // Parse any markdown content after inserting HTML
                parseMarkdownContent(targetElement);
            }
            return html;
        })
        .catch(error => {
            console.error('Error loading partial view:', error);
            throw error;
        });
}
