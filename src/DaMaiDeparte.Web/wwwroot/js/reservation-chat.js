// Ada-ncoa — live-ish messaging on a reservation's Details page.
// No SignalR/WebSockets: this thread is a handful of pickup-coordination messages between two
// people, so simple polling is plenty, and it avoids adding a persistent-connection dependency
// to a single small container. Polling pauses while the tab is hidden.
(function () {
    'use strict';

    var thread = document.getElementById('messageThread');
    var form = document.querySelector('[data-message-form]');
    if (!thread || !form) {
        return;
    }

    var messagesUrl = thread.dataset.messagesUrl;
    var knownIds = new Set();
    thread.querySelectorAll('[data-message-id]').forEach(function (el) {
        knownIds.add(el.dataset.messageId);
    });

    function isScrolledToBottom() {
        return thread.scrollHeight - thread.scrollTop - thread.clientHeight < 40;
    }

    function renderMessage(message) {
        var bubble = document.createElement('div');
        bubble.className = 'p-2 rounded ' + (message.isMine ? 'bg-success bg-opacity-10 align-self-end' : 'bg-light align-self-start');
        bubble.style.maxWidth = '85%';
        bubble.dataset.messageId = String(message.id);

        var meta = document.createElement('div');
        meta.className = 'small text-muted mb-1';
        var strong = document.createElement('strong');
        strong.textContent = message.senderName;
        meta.appendChild(strong);
        meta.appendChild(document.createTextNode(' · ' + message.createdAtDisplay));
        bubble.appendChild(meta);

        var body = document.createElement('div');
        body.className = 'text-break';
        body.style.whiteSpace = 'pre-line';
        body.textContent = message.body; // textContent — never render message bodies as HTML.
        bubble.appendChild(body);

        return bubble;
    }

    function poll() {
        if (document.visibilityState !== 'visible' || !messagesUrl) {
            return;
        }

        fetch(messagesUrl, { headers: { Accept: 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (messages) {
                if (!messages || !messages.length) {
                    return;
                }

                var stickToBottom = isScrolledToBottom();
                var appended = false;

                messages.forEach(function (message) {
                    var id = String(message.id);
                    if (knownIds.has(id)) {
                        return;
                    }
                    knownIds.add(id);
                    thread.appendChild(renderMessage(message));
                    appended = true;
                });

                if (appended) {
                    var placeholder = thread.querySelector('[data-empty-placeholder]');
                    if (placeholder) {
                        placeholder.remove();
                    }
                    if (stickToBottom) {
                        thread.scrollTop = thread.scrollHeight;
                    }
                }
            })
            .catch(function () {
                // A missed poll just tries again on the next tick; nothing to surface to the user.
            });
    }

    var intervalId = window.setInterval(poll, 4000);

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            poll();
        }
    });

    form.addEventListener('submit', function (event) {
        var textarea = form.querySelector('textarea[name="messageBody"]');
        if (!textarea || !textarea.value.trim()) {
            return; // Let the native `required` validation handle this.
        }

        event.preventDefault();
        var button = form.querySelector('button[type="submit"]');
        if (button) {
            button.disabled = true;
        }

        fetch(form.action || window.location.href, { method: 'POST', body: new FormData(form) })
            .then(function () {
                textarea.value = '';
                poll();
            })
            .catch(function () {
                // Fall back to a normal submit (full page reload) if the request itself failed.
                form.submit();
            })
            .finally(function () {
                if (button) {
                    button.disabled = false;
                }
                textarea.focus();
            });
    });

    window.addEventListener('beforeunload', function () {
        window.clearInterval(intervalId);
    });
})();
