(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-sales-history]');
    const tableSelector = '#SalesHistoryTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const canViewProfit = page.dataset.canViewProfit === 'true';

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    const columnDefs = [
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
            data: 'totalRevenueAmount',
            className: 'text-end text-nowrap',
            render: function (data) {
                return encode(data);
            }
        }
    ];

    if (canViewProfit) {
        columnDefs.push({
            data: 'totalProfitAmount',
            className: 'text-end text-nowrap',
            render: function (data) {
                return encode(data);
            }
        });
    }

    columnDefs.push({
        data: null,
        orderable: false,
        className: 'text-end',
        render: function (_data, _type, row) {
            return '<a class="btn btn-sm btn-outline-secondary" href="' +
                abp.appPath + 'Sales/Details/' + encodeURIComponent(row.id) + '">' +
                encode(l('Details')) + '</a>';
        }
    });

    $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Sales/History?handler=List',
                type: 'GET',
                data: input
            });
        }),
        columnDefs: columnDefs
    }));
})();
