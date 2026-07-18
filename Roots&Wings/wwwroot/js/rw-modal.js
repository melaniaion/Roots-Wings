// Generic confirmation pop-up.
// Trigger:  <button data-rw-modal-open="myModalId">
// Cancel:   any [data-rw-modal-close] inside the overlay
// Dismiss:  click on backdrop, or Escape
document.addEventListener("DOMContentLoaded", function () {

    function openModal(overlay) {
        overlay.classList.add("is-active");
        document.body.classList.add("rw-modal-open");
    }

    function closeModal(overlay) {
        overlay.classList.remove("is-active");
        document.body.classList.remove("rw-modal-open");
    }

    // Open triggers
    document.querySelectorAll("[data-rw-modal-open]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var overlay = document.getElementById(btn.getAttribute("data-rw-modal-open"));
            if (overlay) openModal(overlay);
        });
    });

    document.querySelectorAll(".rw-modal-overlay").forEach(function (overlay) {

        // Cancel buttons
        overlay.querySelectorAll("[data-rw-modal-close]").forEach(function (btn) {
            btn.addEventListener("click", function () { closeModal(overlay); });
        });

        // Click outside the card
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) closeModal(overlay);
        });
    });

    // Escape key closes whatever is open
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Escape") return;
        document.querySelectorAll(".rw-modal-overlay.is-active")
            .forEach(function (o) { closeModal(o); });
    });
});