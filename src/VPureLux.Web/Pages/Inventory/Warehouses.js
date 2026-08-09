(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-warehouses-page]');

    if (page && page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    var editModalElement = document.getElementById('WarehouseEditModal');
    var editModal = editModalElement && window.bootstrap
        ? new bootstrap.Modal(editModalElement)
        : null;
    var editForm = document.querySelector('[data-warehouse-edit-form]');
    var editId = document.querySelector('[data-warehouse-edit-id]');
    var editName = document.querySelector('[data-warehouse-edit-name]');
    var editAddress = document.querySelector('[data-warehouse-edit-address]');

    function showEditModal() {
        if (editModal) {
            editModal.show();
            return;
        }

        $(editModalElement).modal('show');
    }

    document.querySelectorAll('[data-warehouse-edit-button]').forEach(function (button) {
        button.addEventListener('click', function () {
            editId.value = button.dataset.id || '';
            editName.value = button.dataset.name || '';
            editAddress.value = button.dataset.address || '';
            showEditModal();
        });
    });

    if (editForm) {
        editForm.addEventListener('submit', function () {
            abp.ui.setBusy(editForm);
        });
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
