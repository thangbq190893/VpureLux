(function () {
    const l = abp.localization.getResource('VPureLux');
    const form = document.querySelector('[data-audit-export-form]');

    if (!form) {
        return;
    }

    form.addEventListener('submit', function (event) {
        if (form.dataset.confirmed === 'true') {
            return;
        }

        event.preventDefault();

        abp.message.confirm(form.dataset.confirmMessage, l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            form.dataset.confirmed = 'true';
            abp.ui.setBusy(form);
            abp.notify.info(form.dataset.startedMessage);
            form.submit();

            // Browser file downloads do not expose a reliable completion callback to this page.
            setTimeout(function () {
                abp.ui.clearBusy(form);
            }, 10000);
        }).catch(function () {
            abp.ui.clearBusy(form);
            abp.notify.error(form.dataset.errorMessage);
        });
    });
})();
