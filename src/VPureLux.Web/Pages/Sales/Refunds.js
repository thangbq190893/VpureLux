(function () {
    const page = document.querySelector('[data-sales-refunds]');
    if (!page) return;
    const l = abp.localization.getResource('VPureLux');
    const modal = new abp.ModalManager({ viewUrl: abp.appPath + 'Sales/RefundModal' });
    const encode = value => $('<div/>').text(value || '').html();
    const table = $('#SalesRefundsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(input => abp.ajax({
            url: abp.appPath + 'Sales/Refunds?handler=List', type: 'GET', data: input
        }), () => ({ searchText: $('#SalesRefundsSearchText').val() })),
        columnDefs: [
            { data: null, orderable: false, render: () => '<button class="btn btn-sm btn-primary js-record-refund">' + encode(l('Sales:RecordRefund')) + '</button>' },
            { data: 'orderNo', render: encode },
            { data: 'customer', render: encode },
            { data: 'remainingAmount', className: 'text-end text-nowrap', render: encode },
            { data: 'createdAt', className: 'text-nowrap', render: encode }
        ]
    }));
    modal.onResult(() => table.ajax.reload(null, false));
    $('#SalesRefundsTable').on('click', '.js-record-refund', function () {
        const row = table.row($(this).closest('tr')).data();
        modal.open({
            taskType: row.taskType,
            operationId: row.operationId,
            orderNo: row.orderNo,
            remainingAmount: row.remainingAmountValue
        });
    });
    $('#SalesRefundsSearchForm').on('submit', function (event) { event.preventDefault(); table.ajax.reload(); });
})();
