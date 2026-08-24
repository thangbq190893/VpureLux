(function () {
    const page = document.querySelector('[data-customer-asset-details]');
    if (!page) return;
    const encode = value => $('<div/>').text(value === null || value === undefined ? '' : value).html();
    $('#AssetMaintenanceHistoryTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [],
        ajax: abp.libs.datatables.createAjax(input => abp.ajax({
            url: abp.appPath + 'Warranty/Assets/Details/' + encodeURIComponent(page.dataset.assetId) + '?handler=History',
            type: 'GET', data: input
        })),
        columnDefs: [
            { data: 'occurredAt', className: 'text-nowrap', render: encode },
            { data: 'eventType', render: encode },
            { data: 'sourceType', render: encode },
            { data: 'componentName', render: (data, _type, row) => row.componentCode ? '<strong>' + encode(row.componentCode) + '</strong><div class="text-muted small">' + encode(data) + '</div>' : '' },
            { data: 'note', orderable: false, render: encode }
        ]
    }));
})();
