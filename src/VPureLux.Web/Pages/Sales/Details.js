(function () {
    const page = document.getElementById('SalesDetailsPage');
    if (!page) {
        return;
    }

    const l = abp.localization.getResource('VPureLux');

    if (page.dataset.salesSuccessMessage) {
        abp.notify.success(page.dataset.salesSuccessMessage);
    }

    page.querySelectorAll('[data-sales-action-form]').forEach(function (form) {
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
                form.submit();
            });
        });
    });
})();
