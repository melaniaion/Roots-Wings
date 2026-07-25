// Opens and closes the prep notes panel overlay.
// Kept separate from rw-modal.js because the panel is a large working
// surface, not a confirm dialog. Same open/close idea, different element.

(function () {
    // Any element with data-rw-panel-open="panelId" opens that panel.
    document.querySelectorAll('[data-rw-panel-open]').forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            var panelId = this.getAttribute('data-rw-panel-open');
            var panel = document.getElementById(panelId);
            if (panel) {
                openPanel(panel);
            }
        });
    });

    // Any element with data-rw-panel-close closes its parent panel.
    document.querySelectorAll('[data-rw-panel-close]').forEach(function (closer) {
        closer.addEventListener('click', function () {
            var panel = this.closest('.rw-panel-overlay');
            if (panel) {
                closePanel(panel);
            }
        });
    });

    // Clicking the dimmed backdrop (but not the card) closes the panel.
    document.querySelectorAll('.rw-panel-overlay').forEach(function (panel) {
        panel.addEventListener('click', function (e) {
            if (e.target === panel) {
                closePanel(panel);
            }
        });
    });

    // Escape closes any open panel.
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var openPanels = document.querySelectorAll('.rw-panel-overlay.is-active');
            openPanels.forEach(closePanel);
        }
    });

    function openPanel(panel) {
        panel.classList.add('is-active');
        document.body.classList.add('rw-panel-open');
    }

    function closePanel(panel) {
        panel.classList.remove('is-active');
        document.body.classList.remove('rw-panel-open');
    }
})();
