(function () {
    var page = document.querySelector('[data-service-details]');
    if (!page) return;
    var id = page.dataset.orderId;
    var l = abp.localization.getResource('VPureLux');
    var encode = function (value) { return $('<div/>').text(value == null ? '' : value).html(); };
    var money = function (value) { return encode(Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' \u20ab'); };
    var method = function (value) { return encode(l('Sales:PaymentMethod:' + ({ 1: 'Cash', 2: 'BankTransfer', 3: 'Card', 4: 'Other' }[value] || value))); };
    function date(value, legacy) {
        if (!value) return '-';
        if (legacy) return encode(value.replace('T', ' ').slice(0, 16));
        var instant = /Z$|[+-]\d{2}:\d{2}$/.test(value) ? value : value + 'Z';
        return encode(new Date(instant).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }));
    }
    var cashModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/MoneyModal' });
    var voidModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/VoidPaymentModal' });
    function refresh() { window.location.reload(); }
    cashModal.onResult(refresh);
    voidModal.onResult(refresh);
    $('#AddServicePayment').on('click', function () { cashModal.open({ id: id, refund: false }); });
    $('#AddServiceRefund').on('click', function () { cashModal.open({ id: id, refund: true }); });
    function ajax(handler) {
        return abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({ url: abp.appPath + 'Service/Details/' + encodeURIComponent(id) + '?handler=' + handler, type: 'GET', data: input });
        }, function () { return { searchText: $('#ServiceMoneySearch').val() }; });
    }
    var payments = $('#ServicePaymentsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [[1, 'desc']],
        ajax: ajax('Payments'), columnDefs: [
            { data: null, orderable: false, visible: page.dataset.managePayments === 'true', rowAction: { items: [
                { text: l('Service:VoidReceipt'), visible: function (data) { var r = data.record || data; return r.status === 1 || r.status === 'Posted'; },
                    action: function (data) { var r = data.record || data; voidModal.open({ id: id, paymentId: r.id, concurrencyStamp: r.concurrencyStamp }); } }
            ] } },
            { data: 'paymentDate', className: 'text-wrap', render: function (value, type, row) { return date(value, row.isLegacy); } },
            { data: 'amount', orderable: false, render: money },
            { data: 'paymentMethod', orderable: false, render: method },
            { data: 'referenceNo', orderable: false, render: encode },
            { data: 'status', render: function (value) { return encode(l(value === 1 || value === 'Posted' ? 'Service:PaymentPosted' : 'Service:PaymentVoided')); } },
            { data: 'note', orderable: false, render: encode },
            { data: 'voidReason', orderable: false, render: function (value, type, row) {
                return encode(value || ((row.status === 2 || row.status === 'Voided') ? l('Service:LegacyVoidReason') : ''));
            } }
        ]
    }));
    var refunds = $('#ServiceRefundsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [[0, 'desc']],
        ajax: ajax('Refunds'), columnDefs: [
            { data: 'refundDate', className: 'text-wrap', render: function (value) { return date(value, false); } },
            { data: 'amount', orderable: false, render: money },
            { data: 'method', orderable: false, render: method },
            { data: 'referenceNo', orderable: false, render: encode },
            { data: 'reason', orderable: false, render: encode }
        ]
    }));
    $('#ServiceMoneyFilters').on('submit', function (e) { e.preventDefault(); payments.ajax.reload(); refunds.ajax.reload(); });
})();
