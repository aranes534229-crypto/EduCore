// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Reusable Bootstrap modal confirmation
// Usage: <form data-confirm-message="Are you sure?" data-confirm-title="Confirm">
//        <button type="submit" data-bs-toggle="modal" data-bs-target="#confirmModal">Action</button>

document.addEventListener('DOMContentLoaded', function () {
    var modalEl = document.getElementById('confirmModal');
    if (!modalEl) return;

    var modal = new bootstrap.Modal(modalEl);
    var messageEl = document.getElementById('confirmModalMessage');
    var titleEl = document.getElementById('confirmModalTitle');
    var submitBtn = document.getElementById('confirmModalSubmit');
    var currentForm = null;

    // Triggered when any element with data-bs-target="#confirmModal" is clicked
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-bs-target="#confirmModal"]');
        if (!trigger) return;

        var form = trigger.closest('form');
        if (!form) return;

        currentForm = form;
        messageEl.textContent = form.getAttribute('data-confirm-message') || 'Are you sure?';
        titleEl.textContent = form.getAttribute('data-confirm-title') || 'Confirm';
        modal.show();
    });

    // When confirm button clicked, submit the form
    submitBtn.addEventListener('click', function () {
        if (currentForm) {
            currentForm.submit();
        }
        modal.hide();
    });

    // Reset on modal hide
    modalEl.addEventListener('hidden.bs.modal', function () {
        currentForm = null;
    });
});