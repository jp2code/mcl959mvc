// Helpers shared by modal pages
function normalizePhoneDigits(s) {
    if (!s) return "";
    let d = s.replace(/\D+/g, "");
    if (d.length === 11 && d[0] === "1") d = d.slice(1);
    return d;
}
function formatUS10(d) {
    if (d.length !== 10) return null;
    return "(" + d.slice(0, 3) + ") " + d.slice(3, 6) + "-" + d.slice(6);
}

// Remove any other initEventPopupUi definitions and keep this ONE
function initEventPopupUi(container) {
    if (!container) return;

    // Calendar
    const cal = container.querySelector('#event-calendar');
    if (cal) {
        const dateStr = cal.getAttribute('data-event-date') || '';
        const nameStr = cal.getAttribute('data-event-name') || '';
        let dt = dateStr ? new Date(dateStr + 'T00:00:00') : new Date();
        if (isNaN(dt.getTime())) dt = new Date();
        cal.setAttribute('title', nameStr + ' on ' + dateStr);
        if (typeof window.renderSimpleCalendar === 'function') {
            window.renderSimpleCalendar('event-calendar', dt, nameStr);
        }
    }

    // Phone inputs in modal (Roster popup, etc.)
    container.querySelectorAll('input[data-phone="true"]').forEach(function (el) {
        // Pre-format any prefilled value
        const d = normalizePhoneDigits(el.value);
        const f = formatUS10(d);
        if (f) el.value = f;

        el.addEventListener('blur', function () {
            const d2 = normalizePhoneDigits(el.value);
            const f2 = formatUS10(d2);
            if (f2) el.value = f2;
        }, { passive: true });
    });
}

// Enhanced calendar: optional titleText parameter
function renderSimpleCalendar(containerId, highlightDate, titleText) {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Accept Date or ISO date string
    let dateObj;
    if (highlightDate instanceof Date) {
        dateObj = highlightDate;
    } else if (typeof highlightDate === 'string' && highlightDate.length >= 10) {
        // Normalize to midnight local
        dateObj = new Date(highlightDate + 'T00:00:00');
        if (isNaN(dateObj.getTime())) dateObj = new Date();
    } else {
        dateObj = new Date();
    }

    const year = dateObj.getFullYear();
    const month = dateObj.getMonth();
    const day = dateObj.getDate();

    const daysOfWeek = ['S','M','T','W','T','F','S'];
    const monthNames = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sept','Oct','Nov','Dec'];

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const headerTitle = titleText
        ? `${titleText}<br/>${monthNames[month]} ${day}, ${year}`
        : `${monthNames[month]} ${day}, ${year}`;

    let html = `<table id="simple-calendar-table" class="mcl959-calendar">
      <tr><th colspan="7">${headerTitle}</th></tr>
      <tr>${daysOfWeek.map(d => `<th>${d}</th>`).join('')}</tr><tr>`;

    for (let i = 0; i < firstDay; i++) html += '<td></td>';

    for (let d = 1; d <= daysInMonth; d++) {
        const isHighlight = (d === day);
        const cellContent = isHighlight
            ? `<span class="day-dot">${d}</span>`
            : `${d}`;
        html += `<td${isHighlight ? ' class="today"' : ''}>${cellContent}</td>`;
        if ((firstDay + d) % 7 === 0 && d !== daysInMonth) html += '</tr><tr>';
    }

    const lastDay = (firstDay + daysInMonth) % 7;
    if (lastDay !== 0) {
        for (let i = lastDay; i < 7; i++) html += '<td></td>';
    }
    html += '</tr></table>';

    container.innerHTML = html;
}

function speakText(text) {
    // cancel any ongoing speech
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1; // normal speed
    utterance.pitch = 1; // normal pitch
    window.speechSynthesis.speak(utterance);
}

// Ensure globally accessible if needed elsewhere
window.renderSimpleCalendar = renderSimpleCalendar;

// (Keep all existing calls to initEventPopupUi after setting modal content)

function getNextMeetingDate(today) {
    let year = today.getFullYear();
    let month = today.getMonth();

    // Helper to get 4th Tuesday of a given month/year
    function fourthTuesday(y, m) {
        // 1st day of month
        let d = new Date(y, m, 1);
        // Find first Tuesday
        let firstTuesday = 1 + ((2 - d.getDay() + 7) % 7);
        // 4th Tuesday is 3 weeks after first
        return new Date(y, m, firstTuesday + 21);
    }

    // Find next meeting month (skip December)
    while (true) {
        // If December, skip to January next year
        if (month === 11) {
            year++;
            month = 0;
            continue;
        }
        let meeting = fourthTuesday(year, month);
        // If meeting is in the future or today, use it
        if (meeting >= today) {
            return meeting;
        }
        // Otherwise, check next month
        month++;
        if (month > 11) {
            month = 0;
            year++;
        }
    }
}

function showRemaining(itemX, statusX, maxchar) {
    console.log('_Layout::showRemaining: ' + itemX);
    const len = itemX.value.length;
    let number = 0;
    if (0 < len) {
        number = maxchar - len;
    } else {
        number = maxchar;
    }
    statusX.textContent = 'Remaining: ' + number;
}

function showCombinedRemaining(itemX, itemY, statusX, maxchar) {
    const lenX = itemX.value.length;
    const lenY = itemY.value.length;
    const lenTot = lenX + lenY;
    if (maxchar < lenTot) {
        return false;
    } else {
        var number = 0;
        if (0 < lenTot) {
            number = maxchar - lenTot;
        } else {
            number = maxchar;
        }
        statusX.textContent = 'Remaining: ' + number;
    }
}

// Central wiring for Bootstrap modal popups and AJAX form handling
(function () {
    if (window.__modalWired) {
        return;
    }
    window.__modalWired = true;

    const getModalEl = () => document.getElementById('entityModal');
    const getContentEl = () => document.getElementById('entityModalContent');

    function getOrCreateModal() {
        const el = getModalEl();
        if (!el) return null;
        return bootstrap.Modal.getOrCreateInstance(el, { backdrop: true, keyboard: true });
    }

    function resolvePopupUrl(trigger) {
        // Highest priority: explicit url on trigger
        if (trigger?.dataset.popupUrl) {
            return trigger.dataset.popupUrl;
        }
        // Next: controller specified on the trigger
        if (trigger?.dataset.popupController) {
            return `/${trigger.dataset.popupController}/Popup`;
        }
        // Optional: page-level defaults
        const content = getContentEl();
        const defaultCtrl =
            content?.dataset.defaultPopupController ||
            document.body.getAttribute('data-roster-popup-controller');
        if (defaultCtrl) {
            return `/${defaultCtrl}/Popup`;
        }
        // Final fallback
        return '/Roster/Popup';
    }

    async function openPopup(popupType, id, urlBase) {
        const modal = getOrCreateModal();
        if (!modal) return;
        const content = getContentEl(); // DEFINE BEFORE 403 HANDLING

        const params = new URLSearchParams({ popupType: popupType || '' });
        if (id) params.append('id', id);

        const resp = await fetch(`${urlBase}?${params.toString()}`, {
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest' // critical for ForbidAjax
            }
        });

        if (resp.status === 403) {
            const html403 = await resp.text();
            // If server gave partial (_AccessDeniedPartial) use directly.
            if (html403.trim() && !html403.includes('<html')) {
                content.innerHTML = html403;
            } else {
                content.innerHTML = '<div class="text-center p-4"><h5 class="text-danger mb-2">Access denied</h5><p class="text-muted mb-0">You do not have access to this resource.</p></div>';
            }
            modal.show();
            return;
        }

        const html = await resp.text();
        if (content) {
            content.innerHTML = html;
            initEventPopupUi(content);
        }
        modal.show();
    }

    // Back-compat: window.openPopup({ controller, action='Popup', popupType, id })
    function openPopupObject(opts) {
        const controller = opts?.controller || document.body.getAttribute('data-roster-popup-controller') || 'Roster';
        const action = opts?.action || 'Popup';
        const urlBase = `/${controller}/${action}`;
        return openPopup(opts?.popupType, opts?.id, urlBase);
    }

    async function postForm(form) {
        const formData = new FormData(form);
        const token =
            formData.get('__RequestVerificationToken') ||
            document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const headers = {};
        headers['X-Requested-With'] = 'XMLHttpRequest';
        if (token) headers['RequestVerificationToken'] = token;

        const resp = await fetch(form.action, {
            method: (form.method || 'POST').toUpperCase(),
            body: formData,
            credentials: 'same-origin',
            headers: headers
        });

        return resp;
    }

    // Open any popup from a trigger
    document.addEventListener('click', (e) => {
        const a = e.target.closest('[data-popup]');
        if (!a) return;

        // Allow links inside modal AND page-level lists
        e.preventDefault();
        const popupType = a.dataset.popup;
        const id = a.dataset.id || a.dataset.itemid; // supports both data-id and data-itemid
        const urlBase = resolvePopupUrl(a);
        openPopup(popupType, id, urlBase);
    });

    document.addEventListener('mouseover', (e) => {
        const readToggle = document.getElementById('readToggle');
        if (readToggle) {
            if (readToggle.checked) {
                // Enable Read Aloud
                const target = e.target;
                const text = target.textContent.trim();
                if (text) {
                    speakText(text);
                }
            } else {
                // Disable Read Aloud
                window.speechSynthesis.cancel();
            }
        }
    });

    // Post any form inside the modal via fetch and re-render (with guards and headers)
    document.addEventListener('submit', async (e) => {
        const content = getContentEl();
        const form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (!content || !content.contains(form)) return; // only handle forms in modal

        // Prevent native submit and double-submit
        if (form.dataset.submitting === 'true') {
            e.preventDefault();
            return;
        }
        e.preventDefault();
        form.dataset.submitting = 'true';

        const submitBtn = form.querySelector('[type="submit"]');
        if (submitBtn) {
            submitBtn.setAttribute('disabled', 'disabled');
        }

        try {
            // Special-case: roster photo upload
            if (form.id === 'photoUploadForm') {
                const respUpload = await postForm(form);

                // 403 handling for photo upload
                if (respUpload.status === 403) {
                    const html403 = await respUpload.text();
                    content.innerHTML = html403.trim() && !html403.includes('<html')
                        ? html403
                        : '<div class="text-center p-4"><h5 class="text-danger mb-2">Access denied</h5><p class="text-muted mb-0">You do not have access to this resource.</p></div>';
                    return;
                }

                const text = await respUpload.text();
                const resultDiv = content.querySelector('#uploadResult');
                if (resultDiv) {
                    resultDiv.innerHTML = text;
                }
                const img = content.querySelector('img[src*="/photos/"]');
                if (img) {
                    const url = new URL(img.getAttribute('src'), window.location.origin);
                    url.searchParams.set('v', Date.now().toString());
                    img.src = `${url.pathname}?${url.searchParams.toString()}`;
                }
                return;
            }

            // Generic AJAX submit
            // Generic AJAX submit (comments, create/edit/delete, delete comment, etc.)
            const resp = await (async () => {
                // Normalize phone inputs (digits only) before creating FormData/posting
                form.querySelectorAll('input[data-phone-normalize="digits"]').forEach(function (el) {
                    el.value = normalizePhoneDigits(el.value);
                });
                return await postForm(form);
            })();

            // 403 (Access Denied) handling BEFORE reading normal body
            if (resp.status === 403) {
                const html403 = await resp.text();
                if (html403.trim() && !html403.includes('<html')) {
                    content.innerHTML = html403;
                } else {
                    const m = html403.match(/<h1[^>]*class="text-danger"[^>]*>(.*?)<\/h1>/i);
                    const p = html403.match(/<p[^>]*class="text-danger"[^>]*>(.*?)<\/p>/i);
                    const heading = m ? m[1] : 'Access denied';
                    const msg = p ? p[1] : 'You do not have access to this resource.';
                    content.innerHTML =
                        `<div class="text-center p-4"><h5 class="text-danger mb-2">${heading}</h5><p class="text-muted mb-0">${msg}</p></div>`;
                }
                return; // stop normal flow
            }

            const ct = resp.headers.get('Content-Type') || '';
            if (ct.includes('application/json')) {
                const data = await resp.json();
                if (data.success) {
                    const modal = getOrCreateModal();
                    if (modal) modal.hide();
                    location.reload();
                    return;
                }
                // If JSON but not success, you could render an error block here.
            }

            const html = await resp.text();
            content.innerHTML = html;
            initEventPopupUi(content);
        } catch (err) {
            console.error('Modal submit error:', err);
            content.innerHTML = '<div class="text-center p-4 text-danger">Unexpected error submitting the form.</div>';
        } finally {
            if (submitBtn) {
                submitBtn.removeAttribute('disabled');
            }
            delete form.dataset.submitting;
        }
    });

    // Memorial convenience (edit/save/cancel/delete) - works across partial reloads
    document.addEventListener('click', async (e) => {
        const content = getContentEl();
        if (!content) return;
        if (!content.contains(e.target)) return;

        // Edit Description
        if (e.target.id === 'editDescription') {
            e.preventDefault();
            const div = content.querySelector('#descriptionDisplay');
            const edit = content.querySelector('#editDescription');
            const save = content.querySelector('#saveDescription');
            const cancel = content.querySelector('#cancelEdit');
            if (div && save && cancel && edit) {
                edit.style.display = 'none';
                save.style.display = '';
                cancel.style.display = '';
                div.contentEditable = 'true';
                div.focus();
            }
            return;
        }

        // Save/Cancel Description (submits hidden form via fetch)
        if (e.target.id === 'saveDescription' || e.target.id === 'cancelEdit') {
            e.preventDefault();
            const form = content.querySelector('#editMemorialForm');
            if (!form) return;
            const div = content.querySelector('#descriptionDisplay');
            const descInput = content.querySelector('#descriptionInput');
            const saveInput = content.querySelector('#saveInput');
            if (div && descInput && saveInput) {
                descInput.value = div.innerHTML;
                saveInput.value = (e.target.id === 'saveDescription') ? 'true' : 'false';
                const resp = await postForm(form);
                const html = await resp.text();
                content.innerHTML = html;
            }
            return;
        }

        // Delete comment (anchor with .delete-comment + hidden form)
        const del = e.target.closest('a.delete-comment');
        if (del) {
            e.preventDefault();
            if (!confirm('Are you sure you want to delete this comment?')) return;
            const id = del.dataset.id || del.dataset.itemid;
            const form = content.querySelector(`#delete-comment-${id}`);
            if (form) {
                const resp = await postForm(form);
                const html = await resp.text();
                content.innerHTML = html;
            }
            return;
        }
    });

    // Expose openPopup for both old and new callers
    window.mcl959 = window.mcl959 || {};
    window.mcl959.openPopup = openPopupObject;
    window.openPopup = openPopupObject;

    // “View last Message” alert wiring (moved from _Layout.cshtml)
    function wireLastMessageAlert() {
        const popup = document.getElementById('popupMessage');
        const closeBtn = document.getElementById('popupCloseBtn');
        const showLink = document.getElementById('showPopupLink');
        const container = document.getElementById('showPopupDiv');
        let timeoutRef;

        if (!popup) {
            if (container) container.classList.add('d-none');
            return;
        }

        // Auto-show briefly on load
        setTimeout(function () { popup.classList.add('show'); }, 10);

        if (closeBtn) {
            closeBtn.addEventListener('click', function (e) {
                e.preventDefault();
                popup.classList.remove('show');
                clearTimeout(timeoutRef);
            });
        }
        if (showLink) {
            showLink.addEventListener('click', function (e) {
                e.preventDefault();
                popup.classList.add('show');
                clearTimeout(timeoutRef);
                timeoutRef = setTimeout(function () {
                    popup.classList.remove('show');
                }, 20000);
            });
        }
    }

    function autoOpenIfPresent() {
        const body = document.body;
        const id = body.getAttribute('data-open-id');
        if (id) {
            const controller = body.getAttribute('data-open-controller') || 'Events';
            const popupType = body.getAttribute('data-open-type') || 'Details';
            if (typeof window.openPopup === 'function') {
                window.openPopup({ controller, action: 'Popup', popupType, id });
            }
        }
    }
      
    // Remember "Read Aloud on Hover" across pages
    function initReadToggle() {
        const KEY = 'mcl959.readToggle';
        const cb = document.getElementById('readToggle');
        if (!cb) {
            return;
        }

        // Load saved state
        const saved = localStorage.getItem(KEY);
        const enabled = (saved === '1' || saved === 'true');
        cb.checked = enabled;

        // Reflect state on <body> (optional CSS/JS hook)
        document.body.classList.toggle('read-enabled', enabled);

        cb.addEventListener('change', function () {
            const isOn = cb.checked;
            localStorage.setItem(KEY, isOn ? '1' : '0');
            document.body.classList.toggle('read-enabled', isOn);
        });
    }

   // One place to initialize page-level features once DOM is ready
    function initPageFeatures() {
        wireLastMessageAlert();
        autoOpenIfPresent();
        initReadToggle();
    }

    // Replace the three separate DOM ready blocks with this one
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPageFeatures);
    } else {
        initPageFeatures();
    }
})();