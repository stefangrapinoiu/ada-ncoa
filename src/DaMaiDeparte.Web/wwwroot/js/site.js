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

    // ---- Photo previews for the single multi-select file field ----
    // Reopening the OS file picker on the SAME <input> replaces its whole selection instead of
    // adding to it (that's native browser behavior, not something `multiple` changes) — so
    // picking photos one at a time used to leave only the last one selected. This keeps its own
    // running list across change events and writes the merged set back into input.files (via
    // DataTransfer) before the form is ever submitted, so photos picked in separate dialog
    // openings all make it into the upload. Each preview also gets a small remove button, since
    // without one a wrong pick could only be undone by reloading the whole form.
    document.querySelectorAll('[data-image-input]').forEach(function (input) {
        var container = document.getElementById(input.dataset.previewContainer);
        if (!container) {
            return;
        }

        var maxFiles = parseInt(input.dataset.maxFiles, 10) || 3;
        var selectedFiles = [];

        function sameFile(a, b) {
            return a.name === b.name && a.size === b.size && a.lastModified === b.lastModified;
        }

        function syncInputFiles() {
            var dataTransfer = new DataTransfer();
            selectedFiles.forEach(function (file) { dataTransfer.items.add(file); });
            input.files = dataTransfer.files;
        }

        function renderPreviews(overflow) {
            container.innerHTML = '';

            if (overflow) {
                var warning = document.createElement('div');
                warning.className = 'col-12 small text-danger';
                warning.textContent = 'Poți adăuga cel mult ' + maxFiles + ' fotografii; restul nu au fost adăugate.';
                container.appendChild(warning);
            }

            selectedFiles.forEach(function (file, index) {
                var col = document.createElement('div');
                col.className = 'col position-relative';

                var img = document.createElement('img');
                img.className = 'img-fluid rounded donation-thumb-wide';
                img.alt = 'Previzualizare fotografia ' + (index + 1);
                col.appendChild(img);

                var remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'btn-close bg-white rounded-circle position-absolute top-0 end-0 m-1 p-1';
                remove.setAttribute('aria-label', 'Elimină fotografia ' + (index + 1));
                remove.addEventListener('click', function () {
                    selectedFiles.splice(index, 1);
                    syncInputFiles();
                    renderPreviews(false);
                });
                col.appendChild(remove);

                container.appendChild(col);

                var reader = new FileReader();
                reader.onload = function (e) { img.src = e.target.result; };
                reader.readAsDataURL(file);
            });
        }

        input.addEventListener('change', function () {
            var incoming = Array.prototype.slice.call(input.files || []);
            var overflow = false;

            incoming.forEach(function (file) {
                if (selectedFiles.length >= maxFiles) {
                    overflow = true;
                    return;
                }
                if (!selectedFiles.some(function (existing) { return sameFile(existing, file); })) {
                    selectedFiles.push(file);
                }
            });

            syncInputFiles();
            renderPreviews(overflow);
        });
    });

    // ---- dd/mm/yyyy and hh:mm typing helpers ----
    // Plain text inputs are used instead of <input type="date">, because a native date field
    // renders in the *browser's* locale (mm/dd/yyyy on a US system) and the page cannot change
    // that. These helpers only insert the separators while typing; the server parses the value.
    function maskInput(input, groups, separator) {
        function format() {
            var digits = input.value.replace(/\D/g, '').slice(0, groups.reduce(function (a, b) { return a + b; }, 0));
            var parts = [];
            var offset = 0;

            groups.forEach(function (size) {
                if (digits.length > offset) {
                    parts.push(digits.substr(offset, size));
                    offset += size;
                }
            });

            input.value = parts.join(separator);
        }

        input.addEventListener('input', function (event) {
            // Let the caret behave normally when deleting.
            if (event.inputType && event.inputType.indexOf('delete') === 0) {
                return;
            }
            format();
        });

        input.addEventListener('blur', format);
    }

    // ---- Calendar / clock pickers (Flatpickr), typing still works either way ----
    // dateFormat/altFormat stay dd/mm/yyyy regardless of the visitor's browser or OS locale,
    // which a native <input type="date"> cannot guarantee. If the Flatpickr CDN script didn't
    // load (offline, blocked), the plain dd/mm/yyyy typing mask below still works.
    if (window.flatpickr && window.flatpickr.l10ns && window.flatpickr.l10ns.ro) {
        window.flatpickr.localize(window.flatpickr.l10ns.ro);
    }

    document.querySelectorAll('[data-date-input]').forEach(function (input) {
        if (window.flatpickr) {
            window.flatpickr(input, {
                dateFormat: 'd/m/Y',
                allowInput: true,
                minDate: input.dataset.minDate || undefined,
                disableMobile: true
            });
        } else {
            maskInput(input, [2, 2, 4], '/');
        }
    });

    document.querySelectorAll('[data-time-input]').forEach(function (input) {
        if (window.flatpickr) {
            window.flatpickr(input, {
                enableTime: true,
                noCalendar: true,
                dateFormat: 'H:i',
                time_24hr: true,
                allowInput: true,
                disableMobile: true
            });
        } else {
            maskInput(input, [2, 2], ':');
        }
    });

    // ---- Country → county → city → neighborhood cascade ----
    // Progressive enhancement only: the server re-validates every parent/child relationship
    // (county belongs to country, city to county, neighborhood to city), so a user without
    // JavaScript is still safe — they just see every option in every select.
    document.querySelectorAll('[data-location-picker]').forEach(function (picker) {
        var country = picker.querySelector('[data-location-country]');
        var county = picker.querySelector('[data-location-county]');
        var city = picker.querySelector('[data-location-city]');
        var neighborhood = picker.querySelector('[data-location-neighborhood]');

        if (!city) {
            return;
        }

        function snapshot(select, parentAttribute) {
            if (!select) {
                return [];
            }
            return Array.prototype.map.call(select.options, function (option) {
                return {
                    value: option.value,
                    text: option.text,
                    parent: option.dataset[parentAttribute] || ''
                };
            });
        }

        var allCounties = snapshot(county, 'country');
        var allCities = snapshot(city, 'county');
        var allNeighborhoods = snapshot(neighborhood, 'city');

        function rebuild(select, source, parentValue, attribute) {
            var previous = select.value;
            select.innerHTML = '';

            source.forEach(function (item) {
                if (item.parent !== '' && item.parent !== parentValue) {
                    return;
                }
                var option = document.createElement('option');
                option.value = item.value;
                option.text = item.text;
                if (item.parent !== '') {
                    option.dataset[attribute] = item.parent;
                }
                select.appendChild(option);
            });

            var stillValid = Array.prototype.some.call(select.options, function (option) {
                return option.value === previous;
            });
            select.value = stillValid ? previous : '';

            // A parent with no children left has only the placeholder option.
            if (select === neighborhood || select === city) {
                select.disabled = select.options.length <= 1;
            }
        }

        function refreshCounties() {
            if (country && county) {
                rebuild(county, allCounties, country.value, 'country');
            }
            refreshCities();
        }

        function refreshCities() {
            if (county) {
                rebuild(city, allCities, county.value, 'county');
            }
            refreshNeighborhoods();
        }

        function refreshNeighborhoods() {
            if (neighborhood) {
                rebuild(neighborhood, allNeighborhoods, city.value, 'city');
            }
        }

        if (country && county) {
            country.addEventListener('change', refreshCounties);
        }
        if (county) {
            county.addEventListener('change', refreshCities);
        }
        city.addEventListener('change', refreshNeighborhoods);

        refreshCounties();
    });

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

    // ---- Keep the bottom nav pinned to the real visible screen on iOS ----
    // `position: fixed` is pinned to the "layout viewport", which can drift out of sync with
    // the actually-visible "visual viewport" on iOS Safari/Chrome while the browser's own
    // toolbars animate in/out during scroll. We track the real visible area ourselves via the
    // visualViewport API and nudge the nav up by the gap, so it stays glued to the bottom of
    // whatever is actually on screen instead of the logical viewport iOS reports.
    var bottomNav = document.querySelector('.bottom-nav');
    if (bottomNav && window.visualViewport) {
        var vv = window.visualViewport;
        var repositionBottomNav = function () {
            var gap = window.innerHeight - (vv.height + vv.offsetTop);
            bottomNav.style.transform = gap > 0.5 ? 'translateY(-' + gap + 'px)' : '';
        };
        vv.addEventListener('resize', repositionBottomNav);
        vv.addEventListener('scroll', repositionBottomNav);
        window.addEventListener('orientationchange', repositionBottomNav);
        repositionBottomNav();
    }

    // ---- Service worker registration (PWA) ----
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').catch(function () {
                // Installation is optional; the site keeps working without it.
            });
        });
    }

    // ---- "Install app" button (Chrome/Edge/Android/desktop) ----
    var deferredInstallPrompt = null;
    var installBtn = document.getElementById('pwaInstallBtn');

    window.addEventListener('beforeinstallprompt', function (event) {
        event.preventDefault();
        deferredInstallPrompt = event;
        if (installBtn) {
            installBtn.classList.remove('d-none');
        }
    });

    if (installBtn) {
        installBtn.addEventListener('click', function () {
            if (!deferredInstallPrompt) {
                return;
            }
            installBtn.classList.add('d-none');
            deferredInstallPrompt.prompt();
            deferredInstallPrompt.userChoice.finally(function () {
                deferredInstallPrompt = null;
            });
        });
    }

    window.addEventListener('appinstalled', function () {
        if (installBtn) {
            installBtn.classList.add('d-none');
        }
        deferredInstallPrompt = null;
    });

    // ---- iOS "Add to Home Screen" tip (Safari has no install prompt API) ----
    var iosTip = document.getElementById('iosInstallTip');
    if (iosTip) {
        var isIos = /iphone|ipad|ipod/i.test(window.navigator.userAgent);
        var isStandalone = window.navigator.standalone === true ||
            window.matchMedia('(display-mode: standalone)').matches;
        var dismissed = false;
        try {
            dismissed = window.localStorage.getItem('adancoa-ios-install-tip-dismissed') === '1';
        } catch (e) {
            dismissed = false;
        }

        if (isIos && !isStandalone && !dismissed) {
            iosTip.classList.remove('d-none');
        }

        var iosTipClose = document.getElementById('iosInstallTipClose');
        if (iosTipClose) {
            iosTipClose.addEventListener('click', function () {
                iosTip.classList.add('d-none');
                try {
                    window.localStorage.setItem('adancoa-ios-install-tip-dismissed', '1');
                } catch (e) {
                    // Ignore storage errors (e.g. private browsing).
                }
            });
        }
    }
})();
