(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-warehouses-page]');

    if (page && page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    document.querySelectorAll('[data-warehouse-status-form]').forEach(function (form) {
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
}());
