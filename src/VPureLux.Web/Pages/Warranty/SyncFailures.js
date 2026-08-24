(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-sync-failures]');
    const tableSelector = '#WarrantySyncFailuresTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const $searchText = $('#WarrantySyncFailureSearchText');
    const $status = $('#WarrantySyncFailureStatus');

    function encode(value) {
        return $('<div/>').text(value === null || value === undefined ? '' : value).html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function tokenHeaders() {
        const token = $('#WarrantySyncFailureTokenForm').find('input[name="__RequestVerificationToken"]').val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function retry(record) {
        abp.message.confirm(l('Warranty:ConfirmSyncRetry'), l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Warranty/SyncFailures?handler=Retry&id=' + encodeURIComponent(record.id),
                type: 'POST',
                headers: tokenHeaders()
            }).then(function () {
                abp.notify.success(l('Warranty:SyncRetryScheduled'));
                dataTable.ajax.reload(null, false);
            });
        });
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
                url: abp.appPath + 'Warranty/SyncFailures?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: $searchText.val(),
                status: $status.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                rowAction: {
                    items: [{
                        text: l('Warranty:RetryNow'),
                        action: function (data) {
                            retry(recordOf(data));
                        }
                    }]
                }
            },
            { data: 'statusLabel', render: encode },
            {
                data: 'orderNo',
                render: function (data, _type, row) {
                    return encode(data) + (row.lineNo ? ' / ' + encode(l('Warranty:LineNo')) + ' ' + encode(row.lineNo) : '');
                }
            },
            {
                data: 'productName',
                render: function (data, _type, row) {
                    return '<strong>' + encode(row.productCode) + '</strong><div class="text-muted small">' + encode(data) + '</div>';
                }
            },
            {
                data: 'errorMessage',
                orderable: false,
                render: function (data, _type, row) {
                    return (row.errorCode ? '<strong>' + encode(row.errorCode) + '</strong><br>' : '') + encode(data);
                }
            },
            { data: 'attemptCount', className: 'text-end', render: encode },
            { data: 'lastOccurredAt', className: 'text-nowrap', render: encode },
            { data: 'nextRetryAt', className: 'text-nowrap', render: encode }
        ]
    }));

    $('#WarrantySyncFailureSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#WarrantySyncFailureClearButton').on('click', function () {
        $searchText.val('');
        $status.val('');
        dataTable.ajax.reload();
    });
})();
