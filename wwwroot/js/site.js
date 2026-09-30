// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Confirm guard: forms with data-confirm="message" ask before submitting.
document.addEventListener('submit', function (e) {
    var form = e.target;
    if (form instanceof HTMLFormElement && form.hasAttribute('data-confirm')
        && !window.confirm(form.getAttribute('data-confirm'))) {
        e.preventDefault();
    }
});
