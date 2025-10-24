// Place this in a <script> tag or a .js file
function renderSimpleCalendar(containerId, highlightDate) {
    const container = document.getElementById(containerId);

    // Use highlightDate's month/year if provided, otherwise use today
    let calendarYear, calendarMonth, highlight;
    let calendarTitle;
    const now = new Date();
    const today = now.getDate();
    const daysOfWeek = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
    const monthNames = [
        'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
        'Jul', 'Aug', 'Sept', 'Oct', 'Nov', 'Dec'
    ];

    if (highlightDate instanceof Date) {
        calendarYear = highlightDate.getFullYear();
        calendarMonth = highlightDate.getMonth();
        highlight = {
            year: calendarYear,
            month: calendarMonth,
            day: highlightDate.getDate()
        };
        calendarTitle = `Next Meeting:<br/>${monthNames[calendarMonth]} ${highlight.day}, ${calendarYear}`;
    } else {
        calendarYear = now.getFullYear();
        calendarMonth = now.getMonth();
        highlight = { year: calendarYear, month: calendarMonth, day: today };
        calendarTitle = `Today:<br/>${monthNames[calendarMonth]} ${highlight.day}, ${calendarYear}`;
    }

    // Get first day of the month and number of days
    const firstDay = new Date(calendarYear, calendarMonth, 1).getDay();
    const daysInMonth = new Date(calendarYear, calendarMonth + 1, 0).getDate();

    let html = `<table id="simple-calendar-table">
    <tr><th colspan="7">${calendarTitle}</th></tr>
    <tr>${daysOfWeek.map(d => `<th>${d}</th>`).join('')}</tr>
    <tr>`;

    // Fill in the blanks before the first day
    for (let i = 0; i < firstDay; i++) html += '<td></td>';

    // Fill in the days
    for (let day = 1; day <= daysInMonth; day++) {
        const isHighlight = (highlight.year === calendarYear && highlight.month === calendarMonth && day === highlight.day);
        html += `<td${isHighlight ? ' class="today"' : ''}>${day}</td>`;
        if ((firstDay + day) % 7 === 0 && day !== daysInMonth) html += '</tr><tr>';
    }

    // Fill in the blanks after the last day
    const lastDay = (firstDay + daysInMonth) % 7;
    if (lastDay !== 0) {
        for (let i = lastDay; i < 7; i++) html += '<td></td>';
    }
    html += '</tr></table>';

    container.innerHTML = html;
}

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
        // If meeting is in the future, use it
        if (meeting > today) {
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
    const getModalEl = () => document.getElementById('entityModal');
    const getContentEl = () => document.getElementById('entityModalContent');

    function getOrCreateModal() {
        const el = getModalEl();
        if (!el) return null;
        return el ? bootstrap.Modal.getOrCreateInstance(el, { backdrop: true, keyboard: true }) : null;
    }

    function resolvePopupUrl(trigger) {
        // Highest priority: data-popup-url on the trigger element
        if (trigger?.dataset.popupUrl) {
            return trigger.dataset.popupUrl;
        }
        // Next priority: data-roster-popup-url on body
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

        const params = new URLSearchParams({ popupType });
        if (id) params.append('id', id);

        const resp = await fetch(`${urlBase}?${params.toString()}`, { credentials: 'same-origin' });
        const html = await resp.text();
        const content = getContentEl();
        if (content) content.innerHTML = html;
        modal.show();
    }

    async function postForm(form) {
        const resp = await fetch(form.action, {
            method: (form.method || 'POST').toUpperCase(),
            body: new FormData(form),
            credentials: 'same-origin'
        });
        const html = await resp.text();
        const content = getContentEl();
        if (content) content.innerHTML = html;
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

    // Post any form inside the modal via fetch and re-render
    document.addEventListener('submit', async (e) => {
        const content = getContentEl();
        const form = e.target;
        if (!content || !content.contains(form)) return; // only handle forms in modal
        e.preventDefault();
        const resp = await fetch(form.action, {
            method: (form.method || 'POST').toUpperCase(),
            body: new FormData(form),
            credentials: 'same-origin'
        });
        const html = await resp.text();
        content.innerHTML = html;
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
                const resp = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
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
            if (form) await postForm(form);
            return;
        }
    });

    // 4) Optional: expose openPopup for manual calls (e.g., page-level buttons)
    window.mcl959 = window.mcl959 || {};
    window.mcl959.openPopup = openPopup;
})();