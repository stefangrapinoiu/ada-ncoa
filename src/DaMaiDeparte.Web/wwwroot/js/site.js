// Ada-ncoa — small progressive enhancements. All security checks happen on the server.
(function () {
    'use strict';

    // ---- Confirmation dialogs for destructive / final actions ----
    // Usage: <form data-confirm="Mesaj" data-confirm-button="Da, anulează" data-confirm-variant="danger">
    var modalElement = document.getElementById('confirmModal');
    var pendingForm = null;

    if (modalElement && window.bootstrap) {
        var modal = new bootstrap.Modal(modalElement);
        var messageElement = document.getElementById('confirmModalMessage');
        var okButton = document.getElementById('confirmModalOk');
        var cancelButton = document.getElementById('confirmModalCancel');

        document.addEventListener('submit', function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || !form.dataset.confirm) {
                return;
            }
            if (form.dataset.confirmed === 'true') {
                return;
            }

            event.preventDefault();
            pendingForm = form;
            messageElement.textContent = form.dataset.confirm;
            okButton.textContent = form.dataset.confirmButton || 'Confirmă';
            cancelButton.textContent = form.dataset.cancelButton || 'Renunță';
            okButton.className = 'btn btn-' + (form.dataset.confirmVariant || 'danger');
            modal.show();
        });

        okButton.addEventListener('click', function () {
            if (!pendingForm) {
                return;
            }
            var form = pendingForm;
            pendingForm = null;
            form.dataset.confirmed = 'true';
            modal.hide();
            if (typeof form.requestSubmit === 'function') {
                form.requestSubmit();
            } else {
                form.submit();
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            pendingForm = null;
        });
    } else {
        // Fallback when Bootstrap JS is unavailable (e.g. offline).
        document.addEventListener('submit', function (event) {
            var form = event.target;
            if (form instanceof HTMLFormElement && form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        });
    }

    // ---- Prevent double submission ----
    document.addEventListener('submit', function (event) {
        if (event.defaultPrevented) {
            return;
        }
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }
        if (window.jQuery && jQuery(form).data('validator') && !jQuery(form).valid()) {
            return;
        }
        window.setTimeout(function () {
            form.querySelectorAll('button[type="submit"]').forEach(function (button) {
                button.disabled = true;
            });
        }, 0);
    });

    // ---- Image preview on donation forms ----
    var imageInput = document.getElementById('imageInput');
    var preview = document.getElementById('imagePreview');
    if (imageInput && preview) {
        imageInput.addEventListener('change', function () {
            var file = imageInput.files && imageInput.files[0];
            if (!file) {
                preview.classList.add('d-none');
                return;
            }
            var reader = new FileReader();
            reader.onload = function (e) {
                preview.src = e.target.result;
                preview.classList.remove('d-none');
            };
            reader.readAsDataURL(file);
        });
    }

    // ---- Auto-submit filters when a select changes ----
    document.querySelectorAll('[data-autosubmit]').forEach(function (select) {
        select.addEventListener('change', function () {
            var form = select.form;
            if (form) {
                var page = form.querySelector('input[name="p"]');
                if (page) {
                    page.value = '1';
                }
                form.submit();
            }
        });
    });

    // ---- Service worker registration (PWA) ----
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').catch(function () {
                // Installation is optional; the site keeps working without it.
            });
        });
    }
})();
