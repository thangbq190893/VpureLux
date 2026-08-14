(function () {
    const l = abp.localization.getResource('VPureLux');
    const tableSelector = '#InventoryLedgerTable';

    if (!document.querySelector(tableSelector)) {
        return;
    }

    const $warehouseId = $('#InventoryLedgerWarehouseId');
    const $stockItemId = $('#InventoryLedgerStockItemId');
    const $type = $('#InventoryLedgerType');
    const $sourceReference = $('#InventoryLedgerSourceReference');
    const $fromDate = $('#InventoryLedgerFromDate');
    const $toDate = $('#InventoryLedgerToDate');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function sourceHtml(row) {
        let html = '<div class="fw-semibold">' + encode(row.sourceLabel) + '</div>';

        if (row.sourceDetail) {
            html += '<div class="text-muted small">' + encode(row.sourceDetail) + '</div>';
        }

        if (row.sourceBomVersionId) {
            html += '<a class="small" href="' + abp.appPath + 'Bom/Details/' +
                encodeURIComponent(row.sourceBomVersionId) + '">' + encode(l('Inventory:SourceOpenBom')) + '</a>';
        }

        return html;
    }

    const dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        language: {
            emptyTable: l('Inventory:NoLedgerEntries'),
            zeroRecords: l('Inventory:NoLedgerEntries')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Inventory/Ledger?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                warehouseId: $warehouseId.val(),
                stockItemId: $stockItemId.val(),
                type: $type.val(),
                sourceReference: $sourceReference.val(),
                fromDate: window.vPureLuxDate ? window.vPureLuxDate.toIso($fromDate.val()) : $fromDate.val(),
                toDate: window.vPureLuxDate ? window.vPureLuxDate.toIso($toDate.val()) : $toDate.val()
            };
        }),
        columnDefs: [
            {
                data: 'postedAt',
                className: 'text-nowrap',
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
                data: 'stockItem',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'type',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'inventory-source-reference',
                render: function (_data, _type, row) {
                    return sourceHtml(row);
                }
            },
            {
                data: 'quantityIn',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'quantityOut',
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
                data: 'amount',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'reason',
                render: function (data) {
                    return encode(data);
                }
            }
        ]
    }));

    $('#InventoryLedgerSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });
})();
