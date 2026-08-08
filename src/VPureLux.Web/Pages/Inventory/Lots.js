(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-inventory-lots-page]');
    var tableSelector = '#InventoryLotsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canUpdateSupplier = page.dataset.canUpdateSupplier === 'true';
    var $warehouseId = $('#InventoryLotsWarehouseId');
    var $stockItemId = $('#InventoryLotsStockItemId');
    var supplierModalElement = document.getElementById('InventoryLotSupplierModal');
    var supplierModal = supplierModalElement && window.bootstrap
        ? new bootstrap.Modal(supplierModalElement)
        : null;
    var $supplierForm = $('#InventoryLotSupplierForm');
    var $lotId = $('#InventoryLotSupplierLotId');
    var $lotLabel = $('#InventoryLotSupplierLotLabel');
    var $supplierId = $('#InventoryLotSupplierId');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function showSupplierModal() {
        if (supplierModal) {
            supplierModal.show();
            return;
        }

        $(supplierModalElement).modal('show');
    }

    function hideSupplierModal() {
        if (supplierModal) {
            supplierModal.hide();
            return;
        }

        $(supplierModalElement).modal('hide');
    }

    function openSupplierModal(record) {
        $lotId.val(record.id);
        $lotLabel.text(record.lotNo + ' - ' + record.stockItem);
        $supplierId.val(record.supplierId || '');
        showSupplierModal();
    }

    var columnDefs = [
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
    ];

    if (canUpdateSupplier) {
        columnDefs.push({
            data: null,
            orderable: false,
            className: 'text-end text-nowrap',
            render: function () {
                return '<button type="button" class="btn btn-outline-primary btn-sm" data-update-lot-supplier>' +
                    encode(l('Inventory:UpdateSupplierShort')) +
                    '</button>';
            }
        });
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
        columnDefs: columnDefs
    }));

    $(tableSelector).on('click', '[data-update-lot-supplier]', function () {
        var record = dataTable.row($(this).closest('tr')).data();
        if (record) {
            openSupplierModal(record);
        }
    });

    $supplierForm.on('submit', function (event) {
        event.preventDefault();

        if (!$supplierId.val()) {
            abp.notify.warn(l('Inventory:SupplierRequired'));
            return;
        }

        abp.ajax({
            url: abp.appPath + 'Inventory/Lots?handler=UpdateSupplier&id=' +
                encodeURIComponent($lotId.val()) +
                '&supplierId=' +
                encodeURIComponent($supplierId.val()),
            type: 'POST'
        }).then(function () {
            abp.notify.success(l('Inventory:LotSupplierUpdatedSuccessfully'));
            hideSupplierModal();
            dataTable.ajax.reload(null, false);
        });
    });

    $('#InventoryLotsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });
})();
