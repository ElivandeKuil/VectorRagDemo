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

var _elaiConversationStarted = false;

async function sendMessage() {
    const input = document.getElementById('userInput');
    const query = input.value.trim();

    if (!query) {
        return;
    }

    // Notify the widget loader (parent frame) that a conversation has started
    if (!_elaiConversationStarted) {
        _elaiConversationStarted = true;
        try { window.parent.postMessage('elai-conversation-started', '*'); } catch (_) {}
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

    const chatContainer = document.querySelector('[data-ask-url]');
    const askUrl = chatContainer ? chatContainer.getAttribute('data-ask-url') : '/Chat/AskEmbed';
    const streamUrl = chatContainer ? chatContainer.getAttribute('data-stream-url') : null;
    const streaming = chatContainer && chatContainer.getAttribute('data-streaming') === 'true' && !!streamUrl;
    const chatHistory = getChatHistory();
    const retrievedChunks = getRetrievedChunks();
    const projectId = parseInt(chatContainer ? chatContainer.getAttribute('data-project-id') || '0' : '0', 10);
    const sessionTokenDiv = document.getElementById('sessionToken');
    const sessionToken = sessionTokenDiv ? (sessionTokenDiv.textContent || '').trim() : '';
    const botName = chatContainer ? (chatContainer.getAttribute('data-bot-name') || 'Assistant') : 'Assistant';

    const requestBody = {
        query: query,
        history: chatHistory,
        retrievedChunks: retrievedChunks,
        projectId: projectId,
        sessionToken: sessionToken || null
    };

    try {
        if (streaming) {
            await sendMessageStreaming(streamUrl, requestBody, messagesContainer, botName, timestamp);
        } else {
            await loadPartialView(askUrl, 'chatMessages', requestBody, 'POST');
        }

        // Scroll to bottom
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }

    } catch (error) {
        console.error('Error:', error);
        const errorDiv = document.createElement('div');
        errorDiv.className = 'message assistant';
        errorDiv.innerHTML = '<div class="message-header"><div class="message-role">System</div><div class="message-timestamp">' + timestamp + '</div></div><div class="message-content">Fout: Communicatie met de server mislukt.</div>';
        messagesContainer.appendChild(errorDiv);
    } finally {
        // Hide loading spinner (may already be hidden by streaming)
        document.getElementById('loadingSpinner').style.display = 'none';
        document.getElementById('sendButton').disabled = false;
        input.focus();
    }
}

async function sendMessageStreaming(streamUrl, requestBody, messagesContainer, botName, timestamp) {
    // Create bot message container immediately (hidden until first token)
    const botMessageDiv = document.createElement('div');
    botMessageDiv.className = 'message assistant';
    botMessageDiv.innerHTML = `
        <div class="message-header">
            <div class="message-role"><i class="bi bi-robot"></i> ${escapeHtml(botName)}</div>
            <div class="message-timestamp">${timestamp}</div>
        </div>
        <div class="message-content markdown-content"></div>
    `;
    // Don't append yet — wait for the first token so spinner stays visible during pre-processing
    const contentEl = botMessageDiv.querySelector('.message-content');
    let botMessageAppended = false;

    let fullText = '';
    let sseBuffer = '';

    const response = await fetch(streamUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody)
    });

    if (!response.ok) {
        throw new Error('Streaming request failed: ' + response.status);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    try {
        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            sseBuffer += decoder.decode(value, { stream: true });

            // Process all complete SSE lines in the buffer
            const lines = sseBuffer.split('\n');
            sseBuffer = lines.pop() || ''; // keep the incomplete last chunk

            for (const line of lines) {
                if (!line.startsWith('data: ')) continue;

                const data = line.slice(6);
                if (data === '[DONE]') return;

                let event;
                try { event = JSON.parse(data); } catch { continue; }

                if (event.type === 'token') {
                    // Show the bot message div and hide the loading spinner on first token
                    if (!botMessageAppended) {
                        messagesContainer.appendChild(botMessageDiv);
                        botMessageAppended = true;
                        document.getElementById('loadingSpinner').style.display = 'none';
                    }
                    fullText += event.content;
                    if (typeof marked !== 'undefined') {
                        contentEl.innerHTML = marked.parse(fullText);
                    } else {
                        contentEl.textContent = fullText;
                    }
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;

                } else if (event.type === 'done') {
                    // Re-render final markdown to ensure completeness
                    if (typeof marked !== 'undefined') {
                        contentEl.innerHTML = marked.parse(fullText);
                    }

                    // Append source links inside the message bubble
                    if (event.sourceLinks && event.sourceLinks.length > 0) {
                        botMessageDiv.appendChild(buildSourceLinksElement(event.sourceLinks));
                    }

                    // Escalation goes in its own bubble, inserted after botMessageDiv
                    if (event.whatsAppUrl || event.emailUrl) {
                        const escalationEl = buildEscalationElement(event);
                        botMessageDiv.insertAdjacentElement('afterend', escalationEl);
                    }

                    // Update hidden state divs used by subsequent requests
                    _updateStateDiv('chatHistory', event.chatHistoryJson);
                    _updateStateDiv('retrievedChunks', event.retrievedChunksJson);
                    _updateStateDiv('conversationId', String(event.conversationId || 0));
                    _ensureStateDiv('sessionToken', messagesContainer, event.sessionToken || '');

                    messagesContainer.scrollTop = messagesContainer.scrollHeight;

                } else if (event.type === 'error') {
                    if (!botMessageAppended) {
                        messagesContainer.appendChild(botMessageDiv);
                        botMessageAppended = true;
                    }
                    contentEl.textContent = 'Fout: ' + (event.message || 'Onbekende fout');
                }
            }
        }
    } finally {
        reader.releaseLock();
    }
}

function _updateStateDiv(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value || '';
}

function _ensureStateDiv(id, container, value) {
    let el = document.getElementById(id);
    if (!el) {
        el = document.createElement('div');
        el.id = id;
        el.style.display = 'none';
        container.appendChild(el);
    }
    el.textContent = value;
}

function buildEscalationElement(event) {
    // Rendered as a separate bubble (sibling to the main bot message div)
    const bubble = document.createElement('div');
    bubble.className = 'message assistant escalation-bubble';

    const label = document.createElement('span');
    label.className = 'escalation-label';
    label.innerHTML = `<i class="bi bi-person-lines-fill"></i>${escapeHtml(event.whatsAppCtaText || event.emailCtaText || 'Neem contact op met mijn collega')}`;
    bubble.appendChild(label);

    const btns = document.createElement('div');
    btns.className = 'escalation-btns';

    if (event.whatsAppUrl) {
        const a = document.createElement('a');
        a.href = event.whatsAppUrl;
        a.target = '_blank';
        a.rel = 'noopener noreferrer';
        a.className = 'whatsapp-cta-btn';
        a.setAttribute('data-log-escalation', 'whatsapp');
        a.innerHTML = `<svg class="cta-btn-icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z"/>
            <path d="M12 0C5.373 0 0 5.373 0 12c0 2.123.558 4.17 1.535 5.943L.055 23.454a.75.75 0 0 0 .918.918l5.538-1.475A11.94 11.94 0 0 0 12 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 22c-1.853 0-3.628-.497-5.163-1.367l-.37-.214-3.835 1.022 1.032-3.764-.228-.376A9.96 9.96 0 0 1 2 12C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10z"/>
        </svg>${escapeHtml(event.whatsAppButtonText || 'Chat via WhatsApp')}`;
        btns.appendChild(a);
    }

    if (event.emailUrl) {
        const a = document.createElement('a');
        a.href = event.emailUrl;
        a.className = 'email-cta-btn';
        a.setAttribute('data-log-escalation', 'email');
        a.innerHTML = `<i class="bi bi-envelope-fill cta-btn-icon"></i>${escapeHtml(event.emailButtonText || 'Stuur een e-mail')}`;
        btns.appendChild(a);
    }

    bubble.appendChild(btns);
    return bubble;
}

function buildSourceLinksElement(sourceLinks) {
    const section = document.createElement('div');
    section.className = 'source-links-section';

    const [first, ...rest] = sourceLinks;

    // Primary card
    section.appendChild(_buildSourceCard(first));

    // If more than one, add a toggle + hidden extras
    if (rest.length > 0) {
        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'source-links-toggle';
        const noun = rest.length === 1 ? 'bron' : 'bronnen';
        toggle.innerHTML = `<i class="bi bi-chevron-down"></i>${escapeHtml(`+${rest.length} ${noun}`)}`;

        const extras = document.createElement('div');
        extras.className = 'source-links-extra';
        extras.hidden = true;

        for (const link of rest) {
            extras.appendChild(_buildSourceChip(link));
        }

        toggle.addEventListener('click', () => {
            extras.hidden = !extras.hidden;
            toggle.classList.toggle('open');
        });

        section.appendChild(toggle);
        section.appendChild(extras);
    }

    return section;
}

function _buildSourceCard(link) {
    const p = link.preview;
    let host = link.url;
    try { host = new URL(link.url).host; } catch (_) {}
    const site  = (p && p.siteName) ? p.siteName : host;
    const title = (p && p.title)    ? p.title    : (link.fallbackTitle || host);

    const a = document.createElement('a');
    a.href = link.url;
    a.target = '_blank';
    a.rel = 'noopener noreferrer';
    a.className = 'source-card';
    a.title = title;

    const favicon = p && p.faviconUrl
        ? `<img src="${escapeHtml(p.faviconUrl)}" alt="" class="source-card-favicon" onerror="this.style.display='none'">`
        : `<i class="bi bi-globe2 source-card-globe"></i>`;

    a.innerHTML = `<div class="source-card-meta">${favicon}<span class="source-card-site">${escapeHtml(site)}</span></div>
        <div class="source-card-title">${escapeHtml(title)}</div>
        ${p && p.description ? `<div class="source-card-desc">${escapeHtml(p.description)}</div>` : ''}`;

    return a;
}

function _buildSourceChip(link) {
    const p = link.preview;
    let host = link.url;
    try { host = new URL(link.url).host; } catch (_) {}
    const label = (p && p.siteName) ? p.siteName : ((p && p.title) ? p.title : host);

    const a = document.createElement('a');
    a.href = link.url;
    a.target = '_blank';
    a.rel = 'noopener noreferrer';
    a.className = 'source-chip';
    a.title = (p && p.title) ? p.title : link.url;

    if (p && p.faviconUrl) {
        a.innerHTML = `<img src="${escapeHtml(p.faviconUrl)}" alt="" class="source-chip-favicon" onerror="this.style.display='none'">`;
    } else {
        a.innerHTML = `<i class="bi bi-link-45deg"></i>`;
    }
    a.innerHTML += `<span>${escapeHtml(label)}</span>`;
    return a;
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
