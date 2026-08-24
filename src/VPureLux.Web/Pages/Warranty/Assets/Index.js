(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-customer-assets]');
    if (!page) return;
    const encode = value => $('<div/>').text(value === null || value === undefined ? '' : value).html();
    const recordOf = data => data && data.record ? data.record : (data || {});
    const $search = $('#CustomerAssetSearchText');
    const $source = $('#CustomerAssetSource');
    const table = $('#CustomerAssetsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [],
        ajax: abp.libs.datatables.createAjax(input => abp.ajax({
            url: abp.appPath + 'Warranty/Assets?handler=List', type: 'GET', data: input
        }), () => ({ searchText: $search.val(), source: $source.val() })),
        columnDefs: [
            { data: null, orderable: false, rowAction: { items: [{
                text: l('Details'), action: data => {
                    const record = recordOf(data);
                    window.location.href = abp.appPath + 'Warranty/Assets/Details/' + encodeURIComponent(record.id);
                }
            }] } },
            { data: 'assetNo', render: encode },
            { data: 'customerName', render: (data, _type, row) => '<strong>' + encode(row.customerCode) + '</strong><div class="text-muted small">' + encode(data) + '</div>' },
            { data: 'model', render: (data, _type, row) => '<strong>' + encode(row.brand) + ' ' + encode(data) + '</strong><div class="text-muted small">' + encode(row.serialNo) + '</div>' },
            { data: 'sourceLabel', render: encode },
            { data: 'positionCount', className: 'text-end', render: encode },
            { data: 'nextDueDate', className: 'text-nowrap', render: encode }
        ]
    }));
    $('#CustomerAssetSearchForm').on('submit', event => { event.preventDefault(); table.ajax.reload(); });
    $('#CustomerAssetClearButton').on('click', () => { $search.val(''); $source.val(''); table.ajax.reload(); });
})();
