(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-pending-installations]');
    const tableSelector = '#PendingInstallationsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const $searchText = $('#PendingInstallationSearchText');

    function encode(value) {
        return $('<div/>').text(value === null || value === undefined ? '' : value).html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    const dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Warranty/PendingInstallations?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return { searchText: $searchText.val() };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                rowAction: {
                    items: [{
                        text: l('Warranty:ConfirmInstallation'),
                        action: function (data) {
                            const record = recordOf(data);
                            window.location.href = abp.appPath + 'Warranty/Install/' + encodeURIComponent(record.id);
                        }
                    }]
                }
            },
            { data: 'assetNo', render: encode },
            {
                data: 'customerName',
                render: function (data, _type, row) {
                    return '<strong>' + encode(row.customerCode) + '</strong><div class="text-muted small">' + encode(data) + '</div>';
                }
            },
            {
                data: 'productName',
                render: function (data, _type, row) {
                    return '<strong>' + encode(row.productCode) + '</strong><div class="text-muted small">' + encode(data) + '</div>';
                }
            },
            {
                data: 'orderNo',
                render: function (data, _type, row) {
                    let detail = row.lineNo ? ' / ' + encode(l('Warranty:LineNo')) + ' ' + encode(row.lineNo) : '';
                    detail += row.unitIndex ? ' / ' + encode(l('Warranty:UnitIndex')) + ' ' + encode(row.unitIndex) : '';
                    return encode(data) + '<div class="text-muted small">' + detail.replace(/^ \/ /, '') + '</div>';
                }
            },
            { data: 'soldDate', className: 'text-nowrap', render: encode },
            { data: 'positionCount', className: 'text-end', render: encode }
        ]
    }));

    $('#PendingInstallationSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#PendingInstallationClearButton').on('click', function () {
        $searchText.val('');
        dataTable.ajax.reload();
    });
})();
