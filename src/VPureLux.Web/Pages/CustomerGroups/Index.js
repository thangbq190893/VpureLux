(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-customer-groups-index]');
    const createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/CreateModal' });
    const editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/EditModal' });
    const detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/DetailsModal' });

    if (page?.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function refreshAfterModal() {
        abp.notify.success(l('CustomerGroups:SavedSuccessfully'));
        location.reload();
    }

    createModal.onResult(refreshAfterModal);
    editModal.onResult(refreshAfterModal);

    document.querySelector('[data-customer-group-create]')?.addEventListener('click', function (event) {
        event.preventDefault();
        createModal.open();
    });

    document.querySelectorAll('[data-customer-group-edit]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            editModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-customer-group-details]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            detailsModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-customer-group-status-form]').forEach(function (form) {
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
