// Ada-ncoa — "Adaugă un aliment" 3-step wizard (Fotografii -> Detalii -> Verifică și publică).
// Client-side only: it's still a single <form> that posts once, from step 3 — the
// PageModel/OnPostAsync behind it is unchanged. If this script never runs (blocked,
// older browser), the "js-wizard-active" class below is never added, so the CSS rule
// that hides inactive steps never applies either: every step just shows at once, as
// one continuous page. Still a fully working form either way; the server re-validates
// everything regardless of what happened here.
(function () {
    'use strict';

    var shell = document.querySelector('[data-wizard]');
    if (!shell) {
        return;
    }

    var form = shell.querySelector('form');
    var steps = Array.prototype.slice.call(shell.querySelectorAll('[data-wizard-step]'));
    if (!form || steps.length === 0) {
        return;
    }

    var segments = Array.prototype.slice.call(shell.querySelectorAll('[data-wizard-seg]'));
    var label = shell.querySelector('[data-wizard-label]');
    var total = steps.length;
    var current = 1;

    var LABELS = {
        1: 'Pasul 1 din ' + total + ' · Fotografii',
        2: 'Pasul 2 din ' + total + ' · Detalii',
        3: 'Pasul 3 din ' + total + ' · Verifică și publică'
    };

    function stepNumber(el) {
        return parseInt(el.getAttribute('data-wizard-step'), 10);
    }

    function stepEl(n) {
        return steps.filter(function (s) { return stepNumber(s) === n; })[0];
    }

    function firstInvalid(step) {
        var fields = step.querySelectorAll('input, select, textarea');
        for (var i = 0; i < fields.length; i++) {
            if (typeof fields[i].checkValidity === 'function' && !fields[i].checkValidity()) {
                return fields[i];
            }
        }
        return null;
    }

    function text(selector) {
        var el = shell.querySelector(selector);
        if (!el) { return ''; }
        if (el.tagName === 'SELECT') {
            return el.selectedIndex >= 0 && el.value ? el.options[el.selectedIndex].text : '';
        }
        return (el.value || '').trim();
    }

    function escapeHtml(value) {
        var div = document.createElement('div');
        div.textContent = value;
        return div.innerHTML;
    }

    function row(icon, label, value) {
        if (!value) { return ''; }
        return '<div class="wizard-review-row"><i class="bi ' + icon + '" aria-hidden="true"></i>' +
            '<div><span class="wizard-review-label">' + label + '</span>' +
            '<span class="wizard-review-value">' + escapeHtml(value) + '</span></div></div>';
    }

    function fillSummary() {
        var target = shell.querySelector('[data-wizard-summary]');
        if (!target) { return; }

        var title = text('#Input_Title') || 'Alimentul tău';
        var category = text('#Input_FoodCategoryId');
        var expiry = text('#Input_ExpirationDate');
        var city = text('#Input_CityId');
        var neighborhood = text('#Input_NeighborhoodId');
        var pickupLocation = text('#Input_PickupLocation');
        var pickupNotes = text('#Input_PickupNotes');
        var locationLabel = [city, neighborhood].filter(Boolean).join(' · ');

        var previewImgs = shell.querySelectorAll('#imagePreviews img');
        var photosHtml = '';
        if (previewImgs.length > 0) {
            photosHtml = '<div class="wizard-review-photos">' +
                Array.prototype.map.call(previewImgs, function (img) {
                    return '<img src="' + img.src + '" alt="" />';
                }).join('') +
                '</div>';
        }

        target.innerHTML =
            photosHtml +
            '<div class="wizard-review-body">' +
                '<div class="wizard-review-title">' + escapeHtml(title) + '</div>' +
                (category ? '<div class="wizard-review-sub">' + escapeHtml(category) + '</div>' : '') +
                row('bi-calendar3', 'Expiră', expiry ? ('pe ' + expiry) : '') +
                '<hr />' +
                row('bi-check-circle', 'Ambalaj', 'Original, sigilat, nedeschis') +
                row('bi-geo-alt', 'Locație', locationLabel) +
                row('bi-signpost', 'Locul predării', pickupLocation) +
                row('bi-chat-left-text', 'Detalii predare', pickupNotes) +
                '<a href="#" class="wizard-review-edit" data-wizard-goto="2"><i class="bi bi-pencil" aria-hidden="true"></i> Modifică detaliile</a>' +
            '</div>';

        var editLink = target.querySelector('[data-wizard-goto]');
        if (editLink) {
            editLink.addEventListener('click', function (event) {
                event.preventDefault();
                show(2);
            });
        }
    }

    function show(n) {
        current = n;
        steps.forEach(function (s) {
            s.classList.toggle('active', stepNumber(s) === n);
        });
        segments.forEach(function (seg) {
            var segN = parseInt(seg.getAttribute('data-wizard-seg'), 10);
            seg.classList.toggle('done', segN <= n);
        });
        if (label && LABELS[n]) {
            label.textContent = LABELS[n];
        }
        if (n === total) {
            fillSummary();
        }
        shell.scrollIntoView({ block: 'start', behavior: 'smooth' });
    }

    shell.querySelectorAll('[data-wizard-next]').forEach(function (button) {
        button.addEventListener('click', function () {
            var step = stepEl(current);
            var invalid = step ? firstInvalid(step) : null;
            if (invalid) {
                invalid.reportValidity();
                return;
            }
            if (current < total) {
                show(current + 1);
            }
        });
    });

    shell.querySelectorAll('[data-wizard-back]').forEach(function (button) {
        button.addEventListener('click', function () {
            if (current > 1) {
                show(current - 1);
            }
        });
    });

    // If the server round-tripped the form with validation errors (JS was off, or a
    // field failed a server-only check), jump to the earliest step that has one
    // instead of defaulting back to step 1 and hiding the problem.
    var errorStep = null;
    steps.forEach(function (s) {
        if (errorStep) { return; }
        var hasError = Array.prototype.some.call(
            s.querySelectorAll('.field-validation-error, .input-validation-error'),
            function (el) { return el.textContent.trim().length > 0 || el.classList.contains('input-validation-error'); }
        );
        if (hasError) {
            errorStep = stepNumber(s);
        }
    });

    document.documentElement.classList.add('js-wizard-active');
    show(errorStep || 1);
})();
