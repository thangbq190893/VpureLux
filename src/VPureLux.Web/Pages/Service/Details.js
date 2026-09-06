(function () {
    const page = document.querySelector('[data-service-details]');
    if (!page) return;
    const l = abp.localization.getResource('VPureLux');
    const id = page.dataset.orderId;
    const concurrencyStamp = page.dataset.concurrencyStamp;
    const cancelModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/CancelModal' });
    cancelModal.onResult(function () { window.location.reload(); });
    const completeModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/CompleteModal',
        scriptUrl: abp.appPath + 'Pages/Service/CompleteModal.js', modalClass: 'ServiceCompletion' });
    completeModal.onResult(function () { window.location.reload(); });
    document.getElementById('CompleteServiceOrder')?.addEventListener('click', function () {
        completeModal.open({ id: id });
    });

    function antiforgeryHeaders() {
        const token = $('#ServiceActionTokenForm').find('input[name="__RequestVerificationToken"]').val();
        return token ? { RequestVerificationToken: token } : {};
    }

    document.getElementById('CancelServiceOrder')?.addEventListener('click', function () {
        cancelModal.open({ id: id, concurrencyStamp: concurrencyStamp });
    });

    document.querySelectorAll('[data-service-action]').forEach(function (button) {
        button.addEventListener('click', function () {
            const action = button.dataset.serviceAction;
            abp.message.confirm(l('Service:' + action + 'Prompt'), l('Confirm')).then(function (confirmed) {
                if (!confirmed) return;
                button.disabled = true;
                abp.ajax({
                    url: abp.appPath + 'Service/Details/' + encodeURIComponent(id) + '?handler=' + action,
                    type: 'POST',
                    headers: antiforgeryHeaders(),
                    data: { concurrencyStamp: concurrencyStamp }
                }).then(function () {
                    window.location.reload();
                }).always(function () {
                    button.disabled = false;
                });
            });
        });
    });
})();
