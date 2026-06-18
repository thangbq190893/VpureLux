(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-customers-index]');
    const createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/CreateModal' });
    const editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/EditModal' });
    const detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/DetailsModal' });

    if (page?.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function refreshAfterModal() {
        abp.notify.success(l('Customers:SavedSuccessfully'));
        location.reload();
    }

    createModal.onResult(refreshAfterModal);
    editModal.onResult(refreshAfterModal);

    document.querySelector('[data-customer-create]')?.addEventListener('click', function (event) {
        event.preventDefault();
        createModal.open();
    });

    document.querySelectorAll('[data-customer-edit]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            editModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-customer-details]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            detailsModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-customer-status-form]').forEach(function (form) {
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
