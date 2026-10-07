/* =========================================================
   ParrotAgent — Knowledge Base Chat
   In-app: cookie session, /api/knowledge-base/{id}/ask
   Embedded: data-parrot-token, /api/embed/{token}/ask
   ========================================================= */
(function () {
    'use strict';

    if (window.__parrotKbChat) return;
    window.__parrotKbChat = true;

    var script = document.currentScript;
    if (!script) {
        var scripts = document.querySelectorAll('script[src*="kb-chat.js"]');
        script = scripts.length ? scripts[scripts.length - 1] : null;
    }
    if (!script) return;

    var scriptUrl = new URL(script.src, window.location.href);
    var origin = scriptUrl.origin;
    var token = (script.dataset.parrotToken || '').trim();
    var embedMode = token.length > 0;
    var endpoint;
    var kbName;

    if (embedMode) {
        endpoint = origin + '/api/embed/' + encodeURIComponent(token) + '/ask';
        kbName = (script.dataset.parrotName || '').trim() || 'this knowledge base';
    } else {
        var holder = document.querySelector('[data-kb-id]');
        var id = holder && holder.getAttribute('data-kb-id');
        if (!id) {
            var segments = window.location.pathname.split('/').filter(Boolean);
            id = segments[segments.length - 1] || '';
        }
        if (!id) return;
        endpoint = '/api/knowledge-base/' + encodeURIComponent(id) + '/ask';
        kbName = (holder && holder.getAttribute('data-kb-name')) || 'this knowledge base';
    }

    var ICONS = {
        comments: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 4h14a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H9l-4 3.2V16H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z"/></svg>',
        robot: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M11 2h2v2h3a2 2 0 0 1 2 2v1h1a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h1V6a2 2 0 0 1 2-2h3V2zm-3 9a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zm8 0a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM8 16h8v1.5a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V16z"/></svg>',
        plus: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6V5z"/></svg>',
        close: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6.4 5 12 10.6 17.6 5 19 6.4 13.4 12 19 17.6 17.6 19 12 13.4 6.4 19 5 17.6 10.6 12 5 6.4 6.4 5z"/></svg>',
        book: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 3h9a3 3 0 0 1 3 3v14a2 2 0 0 0-2-2H5V3zm11 16a1 1 0 0 1 1 1H7.5A2.5 2.5 0 0 0 5 22.5V5h.2A2 2 0 0 1 7 6.8V19h9z"/></svg>',
        send: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 4l7 16-7-3.2L5 20l7-16z"/></svg>',
        file: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 2h8l6 6v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2zm7 1.5V9h5.5L13 3.5z"/></svg>'
    };

    function icon(name) {
        var el = document.createElement('span');
        el.className = 'kbv-chat-icon';
        el.innerHTML = ICONS[name] || ICONS.file;
        return el;
    }

    var host = document.createElement('div');
    host.setAttribute('data-parrot-chat', '');
    var shadow = host.attachShadow({ mode: 'open' });

    var style = document.createElement('link');
    style.rel = 'stylesheet';
    style.href = origin + '/css/kb-chat.css';
    shadow.appendChild(style);

    var fab = document.createElement('button');
    fab.type = 'button';
    fab.className = 'kbv-chat-fab';
    fab.setAttribute('aria-label', 'Open chat');
    fab.appendChild(icon('comments'));

    var chat = document.createElement('aside');
    chat.className = 'kbv-chat';
    chat.hidden = true;

    var header = document.createElement('header');
    header.className = 'kbv-chat-header';

    var headerInfo = document.createElement('div');
    headerInfo.className = 'kbv-chat-header-info';
    var avatar = document.createElement('span');
    avatar.className = 'kbv-chat-avatar';
    avatar.appendChild(icon('robot'));
    var titles = document.createElement('div');
    var title = document.createElement('strong');
    title.textContent = 'Ask ParrotAgent';
    var status = document.createElement('small');
    var online = document.createElement('span');
    online.className = 'kbv-chat-online';
    status.appendChild(online);
    status.appendChild(document.createTextNode(' Online'));
    titles.appendChild(title);
    titles.appendChild(status);
    headerInfo.appendChild(avatar);
    headerInfo.appendChild(titles);

    var headerActions = document.createElement('div');
    headerActions.className = 'kbv-chat-header-actions';
    var newBtn = document.createElement('button');
    newBtn.type = 'button';
    newBtn.className = 'kbv-chat-icon-btn';
    newBtn.setAttribute('aria-label', 'New chat');
    newBtn.appendChild(icon('plus'));
    var closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'kbv-chat-icon-btn';
    closeBtn.setAttribute('aria-label', 'Close chat');
    closeBtn.appendChild(icon('close'));
    headerActions.appendChild(newBtn);
    headerActions.appendChild(closeBtn);
    header.appendChild(headerInfo);
    header.appendChild(headerActions);

    var context = document.createElement('div');
    context.className = 'kbv-chat-context';
    context.appendChild(icon('book'));
    var contextText = document.createElement('span');
    contextText.appendChild(document.createTextNode('Searching in '));
    var contextName = document.createElement('strong');
    contextName.textContent = kbName;
    contextText.appendChild(contextName);
    context.appendChild(contextText);

    var messages = document.createElement('div');
    messages.className = 'kbv-chat-messages';

    var greeting = document.createElement('div');
    greeting.className = 'kbv-chat-msg bot';
    var greetAvatar = document.createElement('span');
    greetAvatar.className = 'kbv-chat-msg-avatar';
    greetAvatar.appendChild(icon('robot'));
    var greetBubble = document.createElement('div');
    greetBubble.className = 'kbv-chat-bubble';
    var greetP = document.createElement('p');
    greetP.textContent = "Hi! I'm your knowledge base assistant. Ask me anything about the documents in this base.";
    greetBubble.appendChild(greetP);
    var suggestions = document.createElement('div');
    suggestions.className = 'kbv-chat-suggestions';
    ['What\'s in this knowledge base?', 'Summarize the latest uploads', 'Find pricing information'].forEach(function (label) {
        var chip = document.createElement('button');
        chip.type = 'button';
        chip.className = 'kbv-chat-suggestion';
        chip.textContent = label;
        suggestions.appendChild(chip);
    });
    greetBubble.appendChild(suggestions);
    greeting.appendChild(greetAvatar);
    greeting.appendChild(greetBubble);
    messages.appendChild(greeting);

    var typing = document.createElement('div');
    typing.className = 'kbv-chat-typing';
    typing.hidden = true;
    typing.innerHTML = '<span></span><span></span><span></span>';

    var form = document.createElement('form');
    form.className = 'kbv-chat-composer';
    var input = document.createElement('textarea');
    input.className = 'kbv-chat-input';
    input.placeholder = 'Ask a question...';
    input.rows = 1;
    input.maxLength = 1000;
    input.autocomplete = 'off';
    var sendBtn = document.createElement('button');
    sendBtn.type = 'submit';
    sendBtn.className = 'kbv-chat-send';
    sendBtn.setAttribute('aria-label', 'Send');
    sendBtn.appendChild(icon('send'));
    form.appendChild(input);
    form.appendChild(sendBtn);

    chat.appendChild(header);
    chat.appendChild(context);
    chat.appendChild(messages);
    chat.appendChild(form);
    shadow.appendChild(fab);
    shadow.appendChild(chat);
    document.body.appendChild(host);

    var backdrop = null;
    var isSending = false;

    function openChat() {
        chat.hidden = false;
        fab.classList.add('is-open');
        if (window.innerWidth <= 640 && !backdrop) {
            backdrop = document.createElement('div');
            backdrop.className = 'kbv-chat-backdrop';
            backdrop.addEventListener('click', closeChat);
            shadow.insertBefore(backdrop, chat);
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
    closeBtn.addEventListener('click', closeChat);

    newBtn.addEventListener('click', function () {
        messages.querySelectorAll('.kbv-chat-msg').forEach(function (m, i) {
            if (i > 0) m.remove();
        });
        input.value = '';
        input.style.height = 'auto';
        input.focus();
    });

    input.addEventListener('input', function () {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 120) + 'px';
    });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        sendMessage();
    });

    suggestions.addEventListener('click', function (e) {
        var chip = e.target.closest('.kbv-chat-suggestion');
        if (!chip) return;
        input.value = chip.textContent.trim();
        sendMessage();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !chat.hidden) closeChat();
    });

    function sendMessage() {
        if (isSending) return;
        var text = (input.value || '').trim();
        if (!text) return;
        if (text.length > 1000) text = text.slice(0, 1000);

        appendUserMessage(text);
        input.value = '';
        input.style.height = 'auto';
        showTyping();
        isSending = true;

        var payload = embedMode
            ? { message: text }
            : { knowledgeBaseId: document.querySelector('[data-kb-id]') && document.querySelector('[data-kb-id]').getAttribute('data-kb-id'), message: text };

        fetch(endpoint, {
            method: 'POST',
            credentials: embedMode ? 'omit' : 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(function (r) {
                return r.json().catch(function () { return {}; }).then(function (data) {
                    return { ok: r.ok, status: r.status, data: data || {} };
                });
            })
            .then(function (res) {
                hideTyping();
                if (res.status === 429) {
                    appendBotMessage('Too many messages. Try again in a minute.');
                } else if (res.status === 403) {
                    appendBotMessage('This chat is not available on this website.');
                } else if (!res.ok) {
                    appendBotMessage(typeof res.data.error === 'string' ? res.data.error : 'Sorry, something went wrong. Please try again.');
                } else {
                    appendBotMessage(typeof res.data.answer === 'string' ? res.data.answer : 'Sorry, something went wrong. Please try again.', embedMode ? [] : (res.data.citations || []));
                }
            })
            .catch(function () {
                hideTyping();
                appendBotMessage(embedMode
                    ? 'This chat is not available on this website.'
                    : 'Sorry, something went wrong. Please try again.');
            })
            .finally(function () { isSending = false; });
    }

    function appendUserMessage(text) {
        var el = document.createElement('div');
        el.className = 'kbv-chat-msg user';
        var bubble = document.createElement('div');
        bubble.className = 'kbv-chat-bubble';
        var p = document.createElement('p');
        p.textContent = text;
        bubble.appendChild(p);
        el.appendChild(bubble);
        messages.appendChild(el);
        scrollToBottom();
    }

    function appendBotMessage(text, citations) {
        var el = document.createElement('div');
        el.className = 'kbv-chat-msg bot';
        var msgAvatar = document.createElement('span');
        msgAvatar.className = 'kbv-chat-msg-avatar';
        msgAvatar.appendChild(icon('robot'));

        var bubble = document.createElement('div');
        bubble.className = 'kbv-chat-bubble';
        var p = document.createElement('p');
        p.textContent = text || '';
        bubble.appendChild(p);

        if (!embedMode && citations && citations.length) {
            citations.forEach(function (c) {
                if (!c || typeof c.name !== 'string') return;
                var card = document.createElement('div');
                card.className = 'kbv-chat-citation';
                card.appendChild(icon('file'));
                var info = document.createElement('div');
                var strong = document.createElement('strong');
                strong.textContent = c.name;
                var small = document.createElement('small');
                small.textContent = typeof c.meta === 'string' ? c.meta : '';
                info.appendChild(strong);
                info.appendChild(small);
                card.appendChild(info);
                bubble.appendChild(card);
            });
        }

        el.appendChild(msgAvatar);
        el.appendChild(bubble);
        messages.appendChild(el);
        scrollToBottom();
    }

    function showTyping() {
        typing.hidden = false;
        messages.appendChild(typing);
        scrollToBottom();
    }

    function hideTyping() {
        typing.hidden = true;
    }

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }
})();
