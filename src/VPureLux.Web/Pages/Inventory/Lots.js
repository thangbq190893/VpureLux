(function () {
    var l = abp.localization.getResource('VPureLux');
    var tableSelector = '#InventoryLotsTable';

    if (!document.querySelector(tableSelector)) {
        return;
    }

    var $warehouseId = $('#InventoryLotsWarehouseId');
    var $stockItemId = $('#InventoryLotsStockItemId');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    var dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        language: {
            emptyTable: l('Inventory:NoLots'),
            zeroRecords: l('Inventory:NoLots')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Inventory/Lots?handler=List',
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
                data: 'lotNo',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'warehouse',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'supplier',
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
                data: 'receivedAt',
                className: 'text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'receivedQuantity',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'availableQuantity',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'unitCost',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'receiptValue',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            }
        ]
    }));

    $('#InventoryLotsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });
})();
