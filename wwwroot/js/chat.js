function getChatHistory() {
    const historyDiv = document.getElementById('chatHistory');
    if (historyDiv && historyDiv.textContent) {
        try {
            return JSON.parse(historyDiv.textContent);
        } catch (e) {
            console.error('Error parsing chat history:', e);
            return [];
        }
    }
    return [];
}

function getRetrievedChunks() {
    const chunksDiv = document.getElementById('retrievedChunks');
    if (chunksDiv && chunksDiv.textContent) {
        try {
            const chunksArray = JSON.parse(chunksDiv.textContent);
            // Parse each serialized chunk string back to an object
            return chunksArray.map(chunkStr => {
                try {
                    return JSON.parse(chunkStr);
                } catch (e) {
                    console.error('Error parsing chunk:', e);
                    return null;
                }
            }).filter(chunk => chunk !== null);
        } catch (e) {
            console.error('Error parsing retrieved chunks:', e);
            return [];
        }
    }
    return [];
}

function handleKeyPress(event) {
    // Send on Enter (without Shift key for new lines)
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        sendMessage();
    }
}

async function sendMessage() {
    const input = document.getElementById('userInput');
    const query = input.value.trim();

    if (!query) {
        return;
    }

    // Clear input
    input.value = '';

    // Get current time for timestamp
    const now = new Date();
    const timestamp = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');

    // Add user message to chat immediately
    const messagesContainer = document.getElementById('chatMessages');
    const userMessageDiv = document.createElement('div');
    userMessageDiv.className = 'message user';
    userMessageDiv.innerHTML = `
        <div class="message-header">
            <div class="message-role">You</div>
            <div class="message-timestamp">${timestamp}</div>
        </div>
        <div class="message-content">${escapeHtml(query)}</div>
    `;
    messagesContainer.appendChild(userMessageDiv);

    // Scroll to bottom to show user message
    messagesContainer.scrollTop = messagesContainer.scrollHeight;

    // Show loading spinner
    document.getElementById('loadingSpinner').style.display = 'block';
    document.getElementById('sendButton').disabled = true;

    try {
        // Get the base URL from the data attribute on the chat container
        const chatContainer = document.querySelector('[data-ask-url]');
        const askUrl = chatContainer ? chatContainer.getAttribute('data-ask-url') : '/Chat/Ask';

        // Extract current chat history and retrieved chunks from hidden divs
        const chatHistory = getChatHistory();
        const retrievedChunks = getRetrievedChunks();

        // Use the global loadPartialView function to load the updated chat panel
        await loadPartialView(
            askUrl,
            'chatMessages',
            {
                query: query,
                history: chatHistory,
                retrievedChunks: retrievedChunks
            },
            'POST'
        );

        // Scroll to bottom
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }

    } catch (error) {
        console.error('Error:', error);
        const errorDiv = document.createElement('div');
        errorDiv.className = 'message assistant';
        errorDiv.innerHTML = '<div class="message-header"><div class="message-role">System</div><div class="message-timestamp">' + timestamp + '</div></div><div class="message-content">Error: Failed to communicate with the server.</div>';
        messagesContainer.appendChild(errorDiv);
    } finally {
        // Hide loading spinner
        document.getElementById('loadingSpinner').style.display = 'none';
        document.getElementById('sendButton').disabled = false;
        input.focus();
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function clearChat() {
    if (confirm('Are you sure you want to clear the chat history?')) {
        const messagesContainer = document.getElementById('chatMessages');
        messagesContainer.innerHTML = '<div class="message assistant">Chat cleared. Ask me anything!</div><div id="chatHistory" style="display:none;">[]</div><div id="retrievedChunks" style="display:none;">[]</div>';
    }
}

function toggleSources(button) {
    const sourcesContent = button.nextElementSibling;
    const isCollapsed = sourcesContent.classList.contains('collapsed');

    if (isCollapsed) {
        sourcesContent.classList.remove('collapsed');
        button.classList.add('expanded');
        button.innerHTML = '<i class="bi bi-book"></i> Hide Sources<i class="bi bi-chevron-down toggle-icon"></i>';
    } else {
        sourcesContent.classList.add('collapsed');
        button.classList.remove('expanded');
        button.innerHTML = '<i class="bi bi-book"></i> Show Sources<i class="bi bi-chevron-down toggle-icon"></i>';
    }
}

// Focus input on load
document.addEventListener('DOMContentLoaded', function() {
    const userInput = document.getElementById('userInput');
    if (userInput) {
        userInput.focus();
    }
});
