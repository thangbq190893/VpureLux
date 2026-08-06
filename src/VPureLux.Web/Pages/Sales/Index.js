(function () {
    const l = abp.localization.getResource('VPureLux');
    const tableSelector = '#SalesOrdersTable';

    if (!document.querySelector(tableSelector)) {
        return;
    }

    const $customerId = $('#SalesCustomerId');
    const $status = $('#SalesStatus');
    const $paymentStatus = $('#SalesPaymentStatus');

    function encode(value) {
        return $('<div/>').text(value || '').html();
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
                url: abp.appPath + 'Sales?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                customerId: $customerId.val(),
                status: $status.val(),
                paymentStatus: $paymentStatus.val()
            };
        }),
        columnDefs: [
            {
                data: 'orderNo',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'orderDate',
                className: 'text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'customerName',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'statusLabel',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'totalRevenueAmount',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'paymentTotalAmount',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'paymentPaidAmount',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'paymentRemainingAmount',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-nowrap',
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.paymentStatusBadgeClass) + '">' +
                        encode(row.paymentStatusLabel) + '</span>';
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                render: function (_data, _type, row) {
                    return '<a class="btn btn-sm btn-outline-secondary" href="' +
                        abp.appPath + 'Sales/Details/' + encodeURIComponent(row.id) + '">' +
                        encode(l('Details')) + '</a>';
                }
            }
        ]
    }));

    $('#SalesSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $paymentStatus.on('change', function () {
        dataTable.ajax.reload();
    });
})();
