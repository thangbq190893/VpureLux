(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-service-index]');
    if (!page) return;
    const encode = value => $('<div/>').text(value == null ? '' : value).html();
    const recordOf = data => data && data.record ? data.record : (data || {});
    const $search = $('#ServiceSearchText');
    const $status = $('#ServiceStatus');
    const table = $('#ServiceOrdersTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [],
        ajax: abp.libs.datatables.createAjax(input => abp.ajax({ url: abp.appPath + 'Service?handler=List', type: 'GET', data: input }),
            () => ({ searchText: $search.val(), status: $status.val() })),
        columnDefs: [
            { data: null, orderable: false, rowAction: { items: [
                { text: l('Details'), action: data => window.location.href = abp.appPath + 'Service/Details/' + encodeURIComponent(recordOf(data).id) },
                { text: l('Service:Cancel'), visible: data => page.dataset.canCancel === 'true' && ['Draft', 'Confirmed', 'InProgress', 1, 2, 3].includes(recordOf(data).status), action: data => {
                    const record = recordOf(data);
                    abp.message.confirm(l('Service:Cancel'), l('Confirm')).then(ok => {
                        if (!ok) return;
                        abp.ajax({ url: abp.appPath + 'Service?handler=Cancel&id=' + encodeURIComponent(record.id), type: 'POST' }).then(() => table.ajax.reload(null, false));
                    });
                }}
            ] } },
            { data: null, name: 'orderNo', render: (_d, _t, row) => '<strong>' + encode(row.orderNo) + '</strong><small class="d-block text-muted">' + encode(row.orderDate) + '</small>' },
            { data: null, name: 'customer', render: (_d, _t, row) => encode(row.customer) + '<small class="d-block text-muted">' + encode(row.asset) + '</small>' },
            { data: null, render: (_d, _t, row) => '<span class="badge ' + encode(row.statusBadge) + '">' + encode(row.statusLabel) + '</span>' },
            { data: null, name: 'revenue', className: 'text-end text-nowrap', render: (_d, _t, row) =>
                '<strong>' + encode(row.revenue) + '</strong><small class="d-block text-muted">' +
                encode(l('Service:Paid')) + ': ' + encode(row.paid) + ' | ' + encode(l('Service:Remaining')) + ': ' + encode(row.remaining) + '</small>' }
        ]
    }));
    $('#ServiceSearchForm').on('submit', event => { event.preventDefault(); table.ajax.reload(); });
    $('#ServiceClearButton').on('click', () => { $search.val(''); $status.val(''); table.ajax.reload(); });
})();
