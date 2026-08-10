(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-catalog-index]');

    if (!page) {
        return;
    }

    const createModal = new abp.ModalManager({ viewUrl: abp.appPath + page.dataset.createViewUrl });
    const editModal = new abp.ModalManager({ viewUrl: abp.appPath + page.dataset.editViewUrl });
    const detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + page.dataset.detailsViewUrl });

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function refreshAfterModal() {
        abp.notify.success(page.dataset.savedMessage);
        if (page.dataset.tableSelector && $.fn.DataTable.isDataTable(page.dataset.tableSelector)) {
            $(page.dataset.tableSelector).DataTable().ajax.reload(null, false);
            return;
        }

        location.reload();
    }

    page.catalogModals = {
        create: createModal,
        edit: editModal,
        details: detailsModal
    };

    createModal.onResult(refreshAfterModal);
    editModal.onResult(refreshAfterModal);

    var createButton = document.querySelector('[data-catalog-create]');
    if (createButton) {
        createButton.addEventListener('click', function (event) {
            event.preventDefault();
            createModal.open();
        });
    }

    document.querySelectorAll('[data-catalog-edit]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            editModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-catalog-details]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            detailsModal.open({ id: link.dataset.id });
        });
    });

    document.querySelectorAll('[data-catalog-status-form]').forEach(function (form) {
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
