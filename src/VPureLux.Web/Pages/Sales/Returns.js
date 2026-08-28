(function () {
    const page = document.querySelector('[data-sales-returns]');
    if (!page) return;
    const l = abp.localization.getResource('VPureLux');
    const modal = new abp.ModalManager({ viewUrl: abp.appPath + 'Sales/ReturnConfirmationModal' });
    const encode = value => $('<div/>').text(value || '').html();
    const table = $('#SalesReturnsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(input => abp.ajax({
            url: abp.appPath + 'Sales/Returns?handler=List', type: 'GET', data: input
        }), () => ({ searchText: $('#SalesReturnsSearchText').val() })),
        columnDefs: [
            { data: null, orderable: false, render: () => '<button class="btn btn-sm btn-primary js-confirm-return">' + encode(l('Sales:ConfirmReturn')) + '</button>' },
            { data: 'orderNo', render: encode },
            { data: 'customer', render: encode },
            { data: 'itemName', render: data => encode(data || l('Sales:EntireOrder')) },
            { data: 'quantity', className: 'text-end', render: encode },
            { data: 'reason', render: encode },
            { data: 'createdAt', className: 'text-nowrap', render: encode }
        ]
    }));
    modal.onResult(() => table.ajax.reload(null, false));
    $('#SalesReturnsTable').on('click', '.js-confirm-return', function () {
        const row = table.row($(this).closest('tr')).data();
        modal.open({
            taskType: row.taskType,
            operationId: row.operationId,
            revisionLineId: row.revisionLineId,
            orderNo: row.orderNo,
            itemName: row.itemName,
            quantity: row.quantity,
            isException: row.isException
        });
    });
    $('#SalesReturnsSearchForm').on('submit', function (event) { event.preventDefault(); table.ajax.reload(); });
})();
