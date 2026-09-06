abp.modals.ServiceCompletion = function () {
    this.initModal = function (manager) {
        const modal = manager.getModal()[0];
        function updateTotal() {
            let total = 0;
            modal.querySelectorAll('[data-price]').forEach(function (row) {
                total += Number(row.dataset.price) * Number(row.querySelector('[data-actual-quantity]').value || 0);
            });
            modal.querySelector('[data-completion-total]').textContent =
                new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(total);
        }
        modal.querySelectorAll('[data-actual-quantity]').forEach(function (input) {
            input.addEventListener('input', updateTotal);
        });
        updateTotal();
    };
};
