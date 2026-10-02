/* =========================================================
   ParrotAgent — Knowledge Base Chat
   wwwroot/js/kb-chat.js
   ========================================================= */
(function () {
    'use strict';

    const url = window.location.pathname;

    // Get path segments without empty strings caused by trailing slashes
    const segments = url.split('/').filter(Boolean);
    const id = segments[segments.length - 1];

    var CHAT_ENDPOINT = '/api/knowledge-base/'+id+'/ask'

    var chat = document.querySelector('[data-kbv-chat]');
    var fab = document.querySelector('[data-kbv-chat-toggle]');
    var closeBtn = document.querySelector('[data-kbv-chat-close]');
    var newBtn = document.querySelector('[data-kbv-chat-new]');
    var form = document.querySelector('[data-kbv-chat-form]');
    var input = document.querySelector('[data-kbv-chat-input]');
    var messages = document.querySelector('[data-kbv-chat-messages]');
    var typing = document.querySelector('[data-kbv-chat-typing]');
    var suggestions = document.querySelectorAll('[data-kbv-chat-suggestion]');

    if (!chat || !fab || !form) return;

    var backdrop = null;
    var isSending = false;

    // ---------------------------------------------------------
    // Open / close
    // ---------------------------------------------------------
    function openChat() {
        chat.hidden = false;
        fab.classList.add('is-open');

        if (window.innerWidth <= 640) {
            backdrop = document.createElement('div');
            backdrop.className = 'kbv-chat-backdrop';
            backdrop.addEventListener('click', closeChat);
            document.body.appendChild(backdrop);
        }

        setTimeout(function () { input.focus(); }, 50);
        scrollToBottom();
    }

    function closeChat() {
        chat.hidden = true;
        fab.classList.remove('is-open');
        if (backdrop) {
            backdrop.remove();
            backdrop = null;
        }
    }

    fab.addEventListener('click', openChat);
    if (closeBtn) closeBtn.addEventListener('click', closeChat);

    if (newBtn) {
        newBtn.addEventListener('click', function () {
            // Clear messages except the first greeting
            var msgs = messages.querySelectorAll('.kbv-chat-msg');
            msgs.forEach(function (m, i) { if (i > 0) m.remove(); });
            input.value = '';
            input.focus();
        });
    }

    // ---------------------------------------------------------
    // Auto-resize textarea
    // ---------------------------------------------------------
    input.addEventListener('input', function () {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 120) + 'px';
    });

    // Enter to send, Shift+Enter for new line
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // ---------------------------------------------------------
    // Form submit
    // ---------------------------------------------------------
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        sendMessage();
    });

    // ---------------------------------------------------------
    // Suggestion chips
    // ---------------------------------------------------------
    suggestions.forEach(function (chip) {
        chip.addEventListener('click', function () {
            input.value = chip.textContent.trim();
            sendMessage();
        });
    });

    // ---------------------------------------------------------
    // Send
    // ---------------------------------------------------------
    function sendMessage() {
        if (isSending) return;

        var text = (input.value || '').trim();
        if (!text) return;

        appendUserMessage(text);
        input.value = '';
        input.style.height = 'auto';
        showTyping();
        isSending = true;

        // ---- Optional: post to the server ----
        // For now, we simulate a response. Swap this block for the fetch call below.

        // simulateBotResponse(text);

        // To hit a real endpoint:
        fetch(CHAT_ENDPOINT, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                knowledgeBaseId: document.querySelector('[data-kb-id]')?.dataset.kbId,
                message: text
            })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            hideTyping();
            appendBotMessage(data.answer, data.citations || []);
        })
        .catch(function () {
            hideTyping();
            appendBotMessage('Sorry, something went wrong. Please try again.');
        })
        .finally(function () { isSending = false; });
    }

    // ---------------------------------------------------------
    // Simulated response (remove when wiring the API)
    // ---------------------------------------------------------
    function simulateBotResponse(question) {
        setTimeout(function () {
            hideTyping();
            var q = question.toLowerCase();

            if (q.indexOf('pricing') !== -1) {
                appendBotMessage(
                    'I found a document about pricing. It outlines a three-tier model with annual discounts.',
                    [{ name: 'pricing-strategy-2025.pdf', meta: '2.4 MB · uploaded 2h ago', type: 'pdf' }]
                );
            } else if (q.indexOf('summary') !== -1 || q.indexOf('summarize') !== -1) {
                appendBotMessage(
                    'This knowledge base currently contains 24 documents covering pricing, product overviews, and customer FAQs. The most recently updated file is pricing-strategy-2025.pdf.'
                );
            } else if (q.indexOf('what') !== -1) {
                appendBotMessage(
                    'This knowledge base contains 24 documents, organized around product documentation, pricing, and customer-facing material.'
                );
            } else {
                appendBotMessage(
                    'I searched across the documents in this base but couldn\'t find a strong match. Try rephrasing, or upload more documents.'
                );
            }

            isSending = false;
        }, 800 + Math.random() * 600);
    }

    // ---------------------------------------------------------
    // Append helpers
    // ---------------------------------------------------------
    function appendUserMessage(text) {
        var el = document.createElement('div');
        el.className = 'kbv-chat-msg user';
        el.innerHTML = '<div class="kbv-chat-bubble"><p></p></div>';
        el.querySelector('p').textContent = text;
        messages.appendChild(el);
        scrollToBottom();
    }

    function appendBotMessage(text, citations) {
        var el = document.createElement('div');
        el.className = 'kbv-chat-msg bot';

        var bubble = document.createElement('div');
        bubble.className = 'kbv-chat-bubble';

        var p = document.createElement('p');
        p.textContent = text;
        bubble.appendChild(p);

        if (citations && citations.length) {
            citations.forEach(function (c) {
                var card = document.createElement('div');
                card.className = 'kbv-chat-citation';

                var iconClass = 'fa-file';
                if (c.type === 'pdf') iconClass = 'fa-file-pdf';
                else if (c.type === 'docx') iconClass = 'fa-file-word';
                else if (c.type === 'xlsx') iconClass = 'fa-file-excel';

                card.innerHTML =
                    '<i class="fas ' + iconClass + '"></i>' +
                    '<div><strong></strong><small></small></div>';
                card.querySelector('strong').textContent = c.name;
                card.querySelector('small').textContent = c.meta || '';

                bubble.appendChild(card);
            });
        }

        var avatar = document.createElement('span');
        avatar.className = 'kbv-chat-msg-avatar';
        avatar.innerHTML = '<i class="fas fa-robot"></i>';

        el.appendChild(avatar);
        el.appendChild(bubble);
        messages.appendChild(el);
        scrollToBottom();
    }

    function showTyping() {
        if (typing) {
            typing.hidden = false;
            messages.appendChild(typing);
            scrollToBottom();
        }
    }

    function hideTyping() {
        if (typing) typing.hidden = true;
    }

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    // ---------------------------------------------------------
    // Escape key closes the chat
    // ---------------------------------------------------------
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !chat.hidden) closeChat();
    });

})();