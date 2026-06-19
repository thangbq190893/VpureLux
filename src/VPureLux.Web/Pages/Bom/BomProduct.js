(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-bom-product]');

    if (page?.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    document.querySelectorAll('[data-bom-action-form]').forEach(function (form) {
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
            }).catch(function () {
                abp.ui.clearBusy(form);
                abp.notify.error(form.dataset.errorMessage);
            });
        });
    });
})();
