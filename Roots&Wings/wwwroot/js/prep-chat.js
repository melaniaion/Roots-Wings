// Roots & Wings Assistant the dialogue.

(function () {
    var config = window.rwChatConfig || {};
    var sessionId = config.sessionId;
    var token = config.antiforgeryToken;
    var endpoint = config.endpoint;

    var transcript = [];        // [{ role, content }]
    var finished = false;
    var selectedTakeaways = [];

    var writingView, chatView, stream, input, sendBtn, textarea, footer, closeX, backBtn;

    function lookup() {
        writingView = document.getElementById('prepWritingView');
        chatView = document.getElementById('prepChatView');
        stream = document.getElementById('rwChatStream');
        input = document.getElementById('rwChatInput');
        sendBtn = document.getElementById('rwChatSend');
        textarea = document.getElementById('prepNotesArea');
        footer = document.getElementById('prepPanelFooter');
        closeX = document.getElementById('prepPanelClose');
        backBtn = document.getElementById('prepPanelBack');
    }

    // Opens the assistant. Always starts a fresh conversation.
    window.rwStartChat = function () {
        lookup();
        resetChat();

        writingView.classList.add('is-hidden');
        chatView.classList.remove('is-hidden');
        if (footer) footer.classList.add('is-hidden');   // no "Save notes" during chat
        if (closeX) closeX.classList.add('is-hidden');   // X hidden during chat
        if (backBtn) backBtn.classList.remove('is-hidden'); // back arrow shown instead

        requestTurn(); // opening question
        wireInput();
    };

    // Wipe any previous conversation so reopening starts clean.
    function resetChat() {
        transcript = [];
        finished = false;
        selectedTakeaways = [];
        if (stream) stream.innerHTML = '';
        // Make sure the input row is visible again (the takeaway step hides it).
        var inputRow = document.querySelector('#prepChatView .rw-chat-input');
        if (inputRow) inputRow.classList.remove('is-hidden');
        if (input) { input.value = ''; input.disabled = false; }
        if (sendBtn) sendBtn.disabled = false;
    }

    // Returns to the writing view. Used by the header back arrow, "Add to my
    // notes" and "Skip".
    function backToWriting() {
        chatView.classList.add('is-hidden');
        writingView.classList.remove('is-hidden');
        if (footer) footer.classList.remove('is-hidden');
        if (closeX) closeX.classList.remove('is-hidden');
        if (backBtn) backBtn.classList.add('is-hidden');
    }
    window.rwBackToWriting = backToWriting;

    // Called when the panel closes, so however it was closed and whatever view
    // it was on, reopening always starts on the writing view.
    window.rwResetToWriting = function () {
        lookup();
        if (chatView && writingView) {
            chatView.classList.add('is-hidden');
            writingView.classList.remove('is-hidden');
            if (footer) footer.classList.remove('is-hidden');
            if (closeX) closeX.classList.remove('is-hidden');
            if (backBtn) backBtn.classList.add('is-hidden');
        }
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
                addMessage('assistant', "Something went wrong reaching the assistant. Please try again later.");
                finished = true;
                renderTakeaways([]);
            });
    }

    // Rendering

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
        // Hide the input row; show the pick-what-to-keep step.
        var inputRow = document.querySelector('#prepChatView .rw-chat-input');
        if (inputRow) inputRow.classList.add('is-hidden');

        var wrap = document.createElement('div');
        wrap.className = 'rw-takeaways';

        if (items.length === 0) {
            var intro0 = document.createElement('p');
            intro0.className = 'rw-takeaways-intro';
            intro0.textContent = 'That is a good place to pause. Head back to your notes whenever you are ready.';
            wrap.appendChild(intro0);

            wrap.appendChild(makeBackButton('Back to my notes'));
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

        // Action row: Add to notes + a plain "Skip" that adds nothing.
        var actions = document.createElement('div');
        actions.style.cssText = 'display:flex; gap:0.6rem; flex-wrap:wrap; margin-top:0.6rem;';

        var addBtn = document.createElement('button');
        addBtn.type = 'button';
        addBtn.className = 'btn-rw-primary';
        addBtn.style.cssText = 'font-size:0.82rem; padding:0.45rem 1.2rem;';
        addBtn.textContent = 'Add to my notes';
        addBtn.addEventListener('click', appendAndReturn);
        actions.appendChild(addBtn);

        var skipBtn = makeBackButton('Skip');
        actions.appendChild(skipBtn);

        wrap.appendChild(actions);
        stream.appendChild(wrap);
        scrollToBottom();
    }

    function makeBackButton(label) {
        var b = document.createElement('button');
        b.type = 'button';
        b.className = 'btn-rw-secondary';
        b.style.cssText = 'font-size:0.82rem; padding:0.45rem 1.2rem;';
        b.textContent = label;
        b.addEventListener('click', backToWriting);
        return b;
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

    // Helpers

    function setInputEnabled(on) {
        input.disabled = !on;
        sendBtn.disabled = !on;
    }

    function scrollToBottom() {
        requestAnimationFrame(function () {
            if (stream) stream.scrollTop = stream.scrollHeight;
        });
    }

    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }
})();