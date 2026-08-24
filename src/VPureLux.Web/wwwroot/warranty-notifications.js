(function () {
    const pollIntervalMilliseconds = 60000;

    function asCount(value) {
        const count = Number(value);
        return Number.isFinite(count) && count > 0 ? Math.floor(count) : 0;
    }

    function displayCount(count) {
        return count > 99 ? '99+' : count.toString();
    }

    function update(root, summary) {
        const warningCount = asCount(summary.warningCount);
        const overdueCount = asCount(summary.overdueCount);
        const totalCount = warningCount + overdueCount;
        const button = root.querySelector('.vpl-warranty-notification__button');
        const badge = root.querySelector('[data-notification-badge]');
        const alertList = root.querySelector('[data-notification-alert-list]');
        const empty = root.querySelector('[data-notification-empty]');
        const overdueLink = root.querySelector('[data-notification-overdue-link]');
        const warningLink = root.querySelector('[data-notification-warning-link]');

        button.classList.toggle('vpl-warranty-notification__button--active', totalCount > 0);
        button.classList.toggle('vpl-warranty-notification__button--overdue', overdueCount > 0);
        badge.textContent = displayCount(totalCount);
        badge.classList.toggle('d-none', totalCount === 0);
        alertList.classList.toggle('d-none', totalCount === 0);
        empty.classList.toggle('d-none', totalCount > 0);
        overdueLink.classList.toggle('d-none', overdueCount === 0);
        warningLink.classList.toggle('d-none', warningCount === 0);
        root.querySelector('[data-notification-overdue-count]').textContent = overdueCount.toString();
        root.querySelector('[data-notification-warning-count]').textContent = warningCount.toString();
    }

    function initialize() {
        const roots = Array.from(document.querySelectorAll('[data-warranty-notification]'));
        if (roots.length === 0) {
            return;
        }

        const endpoint = roots[0].dataset.endpoint;
        let refreshing = false;

        async function refresh() {
            if (refreshing || document.visibilityState === 'hidden') {
                return;
            }

            refreshing = true;
            try {
                const response = await fetch(endpoint, {
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });
                if (!response.ok) {
                    return;
                }

                const summary = await response.json();
                roots.forEach(function (root) { update(root, summary); });
            } catch (_ignored) {
                // Keep the current badge when a background refresh is temporarily unavailable.
            } finally {
                refreshing = false;
            }
        }

        window.setInterval(refresh, pollIntervalMilliseconds);
        document.addEventListener('visibilitychange', function () {
            if (document.visibilityState === 'visible') {
                refresh();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})();
