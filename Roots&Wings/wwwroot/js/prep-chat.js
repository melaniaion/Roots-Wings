// Roots & Wings Assistant — the reflection dialogue.
// Holds the conversation in the browser and posts the whole transcript to
// the server each turn, so the server stays stateless. Renders messages,
// a typing indicator, and the closing takeaway chips.

(function () {
    // Elements are looked up lazily when the chat starts, because the panel
    // markup is on the page from the beginning but only used on demand.
    var config = window.rwChatConfig || {};
    var sessionId = config.sessionId;
    var token = config.antiforgeryToken;
    var endpoint = config.endpoint;

    var transcript = [];      // [{ role: 'assistant'|'user', content: '...' }]
    var finished = false;
    var selectedTakeaways = [];

    var writingView, chatView, stream, input, sendBtn, textarea;

    // Called when the user clicks the assistant invitation.
    window.rwStartChat = function () {
        writingView = document.getElementById('prepWritingView');
        chatView = document.getElementById('prepChatView');
        stream = document.getElementById('rwChatStream');
        input = document.getElementById('rwChatInput');
        sendBtn = document.getElementById('rwChatSend');
        textarea = document.getElementById('prepNotesArea');

        // Swap writing view for chat view within the same panel.
        writingView.classList.add('is-hidden');
        chatView.classList.remove('is-hidden');

        // If we already have a conversation (user reopened), don't restart.
        if (transcript.length === 0) {
            requestTurn(); // opening question
        }

        wireInput();
    };

    function wireInput() {
        if (sendBtn.dataset.wired) return;
        sendBtn.dataset.wired = "true";

        sendBtn.addEventListener('click', sendUserMessage);
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendUserMessage();
            }
        });
    }

    function sendUserMessage() {
        var text = input.value.trim();
        if (!text || finished) return;

        addMessage('user', text);
        transcript.push({ role: 'user', content: text });
        input.value = '';

        requestTurn();
    }

    // Posts the transcript, gets the next assistant reply.
    function requestTurn() {
        setInputEnabled(false);
        showTyping();

        fetch(endpoint + '?id=' + sessionId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(transcript)
        })
            .then(function (res) { return res.json(); })
            .then(function (reply) {
                hideTyping();

                if (reply.isFinal) {
                    // Assistant's closing message, then the takeaway step.
                    if (reply.message) {
                        addMessage('assistant', reply.message);
                        transcript.push({ role: 'assistant', content: reply.message });
                    }
                    finished = true;
                    renderTakeaways(reply.takeaways || []);
                } else {
                    addMessage('assistant', reply.message);
                    transcript.push({ role: 'assistant', content: reply.message });
                    setInputEnabled(true);
                    input.focus();
                }
            })
            .catch(function () {
                hideTyping();
                addMessage('assistant', "Something went wrong reaching the assistant. You can keep writing on your own.");
                finished = true;
            });
    }

    // ── Rendering ──

    function addMessage(role, text) {
        var row = document.createElement('div');
        row.className = 'rw-msg rw-msg-' + role;

        if (role === 'assistant') {
            var mark = document.createElement('div');
            mark.className = 'rw-msg-mark';
            mark.textContent = '✦';
            row.appendChild(mark);
        }

        var bubble = document.createElement('div');
        bubble.className = 'rw-msg-bubble';
        bubble.textContent = text;
        row.appendChild(bubble);

        stream.appendChild(row);
        scrollToBottom();
    }

    var typingEl = null;
    function showTyping() {
        typingEl = document.createElement('div');
        typingEl.className = 'rw-msg rw-msg-assistant';
        typingEl.innerHTML =
            '<div class="rw-msg-mark">✦</div>' +
            '<div class="rw-msg-bubble rw-typing"><span></span><span></span><span></span></div>';
        stream.appendChild(typingEl);
        scrollToBottom();
    }

    function hideTyping() {
        if (typingEl) { typingEl.remove(); typingEl = null; }
    }

    function renderTakeaways(items) {
        // Hide the input; show the pick-what-to-keep step.
        input.parentElement.classList.add('is-hidden');

        var wrap = document.createElement('div');
        wrap.className = 'rw-takeaways';

        if (items.length === 0) {
            var doneOnly = document.createElement('button');
            doneOnly.type = 'button';
            doneOnly.className = 'btn-rw-secondary';
            doneOnly.style.cssText = 'font-size:0.82rem; padding:0.45rem 1.2rem;';
            doneOnly.textContent = 'Back to my notes';
            doneOnly.addEventListener('click', backToWriting);
            wrap.appendChild(doneOnly);
            stream.appendChild(wrap);
            scrollToBottom();
            return;
        }

        var intro = document.createElement('p');
        intro.className = 'rw-takeaways-intro';
        intro.textContent = 'Tick anything that feels worth carrying into your notes:';
        wrap.appendChild(intro);

        items.forEach(function (item) {
            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'rw-takeaway-chip';
            chip.innerHTML = '<span class="rw-takeaway-check">✓</span><span>' + escapeHtml(item) + '</span>';
            chip.addEventListener('click', function () {
                chip.classList.toggle('selected');
                if (chip.classList.contains('selected')) {
                    selectedTakeaways.push(item);
                } else {
                    selectedTakeaways = selectedTakeaways.filter(function (t) { return t !== item; });
                }
            });
            wrap.appendChild(chip);
        });

        var addBtn = document.createElement('button');
        addBtn.type = 'button';
        addBtn.className = 'btn-rw-primary';
        addBtn.style.cssText = 'font-size:0.82rem; padding:0.45rem 1.2rem; margin-top:0.6rem;';
        addBtn.textContent = 'Add to my notes';
        addBtn.addEventListener('click', appendAndReturn);
        wrap.appendChild(addBtn);

        stream.appendChild(wrap);
        scrollToBottom();
    }

    function appendAndReturn() {
        if (selectedTakeaways.length > 0) {
            var existing = textarea.value.trim();
            var addition = selectedTakeaways.map(function (t) { return '• ' + t; }).join('\n');
            textarea.value = existing.length > 0
                ? existing + '\n' + addition
                : addition;
        }
        backToWriting();
    }

    function backToWriting() {
        chatView.classList.add('is-hidden');
        writingView.classList.remove('is-hidden');
    }

    // ── Helpers ──

    function setInputEnabled(on) {
        input.disabled = !on;
        sendBtn.disabled = !on;
    }

    function scrollToBottom() {
        // Defer to next frame so the new message is laid out first, then
        // scroll only the stream (not the panel) to its bottom.
        requestAnimationFrame(function () {
            if (stream) {
                stream.scrollTop = stream.scrollHeight;
            }
        });
    }

    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }
})();