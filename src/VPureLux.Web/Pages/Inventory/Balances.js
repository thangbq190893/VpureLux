(function () {
    const l = abp.localization.getResource('VPureLux');
    const tableSelector = '#InventoryBalancesTable';

    if (!document.querySelector(tableSelector)) {
        return;
    }

    const $warehouseId = $('#InventoryBalancesWarehouseId');
    const $stockItemId = $('#InventoryBalancesStockItemId');

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
        language: {
            emptyTable: l('Inventory:NoBalances'),
            zeroRecords: l('Inventory:NoBalances')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Inventory/Balances?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                warehouseId: $warehouseId.val(),
                stockItemId: $stockItemId.val()
            };
        }),
        columnDefs: [
            {
                data: 'warehouse',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'stockItem',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'quantityOnHand',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'inventoryValue',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    const url = abp.appPath + 'Inventory/Lots?WarehouseId=' +
                        encodeURIComponent(row.warehouseId) + '&StockItemId=' +
                        encodeURIComponent(row.stockItemId);
                    return '<a class="btn btn-sm btn-outline-secondary" href="' + url + '">' +
                        encode(l('Inventory:ViewReceiptLotHistory')) + '</a>';
                }
            }
        ]
    }));

    $('#InventoryBalancesSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });
})();
