document.querySelectorAll('[data-catalog-image-input]').forEach(function (input) {
    input.addEventListener('change', function () {
        const preview = document.querySelector(input.dataset.previewTarget);
        if (!preview || !input.files || input.files.length === 0) {
            return;
        }

        preview.src = URL.createObjectURL(input.files[0]);
        preview.classList.remove('d-none');
    });
});

document.querySelectorAll('[data-catalog-image-page]').forEach(function (page) {
    if (page.dataset.imageActionSuccess) {
        abp.notify.success(page.dataset.imageActionSuccess);
    }
});

document.querySelectorAll('[data-catalog-image-remove-form]').forEach(function (form) {
    form.addEventListener('submit', function (event) {
        event.preventDefault();

        abp.message.confirm(form.dataset.confirmMessage, abp.localization.getResource('VPureLux')('Confirm'))
            .then(function (confirmed) {
                if (!confirmed) {
                    return;
                }

                abp.ui.setBusy(form);
                form.submit();
            });
    });
});
