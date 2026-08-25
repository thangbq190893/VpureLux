(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-service-details]');
    if (!page) return;
    const id = page.dataset.orderId;
    const completeModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/CompleteModal' });
    const paymentModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/PaymentModal' });
    const token = $('#ServiceDetailsTokenForm').find('input[name="__RequestVerificationToken"]').val();
    const tokenHeaders = token ? { RequestVerificationToken: token } : {};
    completeModal.onResult(() => window.location.reload()); paymentModal.onResult(() => window.location.reload());
    document.getElementById('CompleteServiceButton')?.addEventListener('click', () => completeModal.open({ id: id }));
    document.getElementById('AddServicePaymentButton')?.addEventListener('click', () => paymentModal.open({ id: id }));
    document.querySelectorAll('[data-void-payment]').forEach(button => button.addEventListener('click', () => {
        abp.message.confirm(l('Service:VoidPaymentConfirm'), l('Confirm')).then(ok => {
            if (!ok) return;
            abp.ajax({
                url: abp.appPath + 'Service/Details/' + encodeURIComponent(id) + '?handler=VoidPayment&paymentId=' + encodeURIComponent(button.dataset.voidPayment),
                type: 'POST',
                headers: tokenHeaders
            }).then(() => window.location.reload());
        });
    }));
    document.querySelectorAll('[data-service-action]').forEach(button => button.addEventListener('click', () => {
        const action = button.dataset.serviceAction;
        abp.message.confirm(l('Service:' + action), l('Confirm')).then(ok => {
            if (!ok) return;
            abp.ajax({ url: abp.appPath + 'Service/Details/' + encodeURIComponent(id) + '?handler=' + action, type: 'POST', headers: tokenHeaders })
                .then(() => window.location.reload());
        });
    }));
})();
