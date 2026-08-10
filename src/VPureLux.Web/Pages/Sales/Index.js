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

    function cancelOrder(row) {
        const message = row.cancelConfirmationMessage || l('Sales:CancelOrderMessage');
        abp.message.confirm(message, l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Sales?handler=Cancel&id=' + encodeURIComponent(row.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(l('Sales:CancelledSuccessfully'));
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
                data: null,
                orderable: false,
                className: 'text-start',
                render: function (_data, _type, row) {
                    let html = '<div class="btn-group btn-group-sm" role="group">' +
                        '<a class="btn btn-outline-secondary" href="' +
                        abp.appPath + 'Sales/Details/' + encodeURIComponent(row.id) + '">' +
                        encode(l('Details')) + '</a>';
                    if (row.canCancel) {
                        html += '<button type="button" class="btn btn-outline-danger js-sales-cancel" data-order-id="' +
                            encode(row.id) + '">' + encode(l('Sales:Cancel')) + '</button>';
                    }

                    return html + '</div>';
                }
            },
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
            }
        ]
    }));

    $(tableSelector).on('click', '.js-sales-cancel', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            cancelOrder(row);
        }
    });

    $('#SalesSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $paymentStatus.on('change', function () {
        dataTable.ajax.reload();
    });
})();
