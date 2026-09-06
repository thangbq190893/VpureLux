(function () {
    const page = document.querySelector('[data-service-index]');
    if (!page) return;
    const l = abp.localization.getResource('VPureLux');
    const encode = function (value) { return $('<div/>').text(value == null ? '' : value).html(); };
    const recordOf = function (data) { return data && data.record ? data.record : (data || {}); };
    const $search = $('#ServiceSearchText');
    const $status = $('#ServiceStatus');
    const $from = $('#ServiceFromDate');
    const $to = $('#ServiceToDate');

    const table = $('#ServiceOrdersTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [[2, 'desc']],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({ url: abp.appPath + 'Service?handler=List', type: 'GET', data: input });
        }, function () {
            return {
                searchText: $search.val(),
                status: $status.val(),
                fromDate: $from.val(),
                toDate: $to.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                rowAction: { items: [
                    {
                        text: l('Details'),
                        action: function (data) {
                            window.location.href = abp.appPath + 'Service/Details/' + encodeURIComponent(recordOf(data).id);
                        }
                    },
                    {
                        text: l('Edit'),
                        visible: function (data) { return recordOf(data).canEdit; },
                        action: function (data) {
                            window.location.href = abp.appPath + 'Service/Edit/' + encodeURIComponent(recordOf(data).id);
                        }
                    }
                ] }
            },
            { data: 'orderNo', name: 'orderNo', render: encode },
            { data: 'orderDate', name: 'orderDate', className: 'text-nowrap', render: encode },
            {
                data: null,
                name: 'customer',
                render: function (_data, _type, row) {
                    return encode(row.customer) + '<small class="d-block text-muted">' + encode(row.asset) + '</small>';
                }
            },
            { data: 'scheduledAt', orderable: false, className: 'text-nowrap', render: encode },
            {
                data: null,
                name: 'status',
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.statusBadge) + '">' + encode(row.statusLabel) + '</span>';
                }
            },
            { data: 'plannedAmount', orderable: false, className: 'text-end text-nowrap', render: encode }
        ]
    }));

    $('#ServiceSearchForm').on('submit', function (event) {
        event.preventDefault();
        table.ajax.reload();
    });
    $('#ServiceClearButton').on('click', function () {
        $search.val('');
        $status.val('');
        $from.val('');
        $to.val('');
        table.ajax.reload();
    });
    $status.on('change', function () { table.ajax.reload(); });
})();
