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
        const askUrl = chatContainer ? chatContainer.getAttribute('data-ask-url') : '/Chat/AskEmbed';

        // Extract current chat history and retrieved chunks from hidden divs
        const chatHistory = getChatHistory();
        const retrievedChunks = getRetrievedChunks();

        const projectId = parseInt(chatContainer.getAttribute('data-project-id') || '0', 10);

        const sessionTokenDiv = document.getElementById('sessionToken');
        const sessionToken = sessionTokenDiv ? (sessionTokenDiv.textContent || '').trim() : '';

        // Use the global loadPartialView function to load the updated chat panel
        await loadPartialView(
            askUrl,
            'chatMessages',
            {
                query: query,
                history: chatHistory,
                retrievedChunks: retrievedChunks,
                projectId: projectId,
                sessionToken: sessionToken || null
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
        messagesContainer.innerHTML = '<div class="message assistant">Chat cleared. Ask me anything!</div><div id="chatHistory" style="display:none;">[]</div><div id="retrievedChunks" style="display:none;">[]</div><div id="conversationId" style="display:none;">0</div>';
    }
}

function toggleSources(header) {
    const messageDiv = header.parentElement;
    const sourcesContent = messageDiv.querySelector('.sources-content');
    const toggleIcon = header.querySelector('.toggle-icon');

    if (!sourcesContent || !toggleIcon) return;

    const isCollapsed = sourcesContent.classList.contains('collapsed');

    if (isCollapsed) {
        sourcesContent.classList.remove('collapsed');
        header.classList.add('expanded');
        toggleIcon.style.transform = 'rotate(180deg)';
    } else {
        sourcesContent.classList.add('collapsed');
        header.classList.remove('expanded');
        toggleIcon.style.transform = 'rotate(0deg)';
    }
}

// Configure marked.js for security and options
if (typeof marked !== 'undefined') {
    marked.setOptions({
        breaks: true,          // Convert \n to <br>
        gfm: true,             // GitHub Flavored Markdown
        headerIds: false,      // Don't add IDs to headers
        mangle: false,         // Don't mangle email addresses
        sanitize: false        // We trust the API responses
    });
}

// Focus input on load and parse any existing markdown content
document.addEventListener('DOMContentLoaded', function() {
    const userInput = document.getElementById('userInput');
    if (userInput) {
        userInput.focus();
    }

    // Parse any markdown content that was rendered on initial page load
    const chatMessages = document.getElementById('chatMessages');
    if (chatMessages && typeof parseMarkdownContent !== 'undefined') {
        parseMarkdownContent(chatMessages);
    }
});

// Escalation-button click tracking (fire-and-forget)
// Listens for clicks on any element with data-log-escalation="whatsapp|email",
// reads the current conversationId from the DOM and POSTs to LogEscalation.
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-log-escalation]');
    if (!btn) return;

    var kanaal = btn.getAttribute('data-log-escalation');
    var conversationIdDiv = document.getElementById('conversationId');
    var conversationId = conversationIdDiv ? parseInt(conversationIdDiv.textContent || '0', 10) : 0;

    if (!kanaal || conversationId <= 0) return;

    fetch('/Chat/LogEscalation?conversationId=' + conversationId + '&kanaal=' + encodeURIComponent(kanaal), {
        method: 'POST'
    }).catch(function () { /* intentionally ignored */ });
});
