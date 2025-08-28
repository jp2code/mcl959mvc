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

