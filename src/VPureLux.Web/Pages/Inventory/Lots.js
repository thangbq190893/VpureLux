(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-inventory-lots-page]');
    var tableSelector = '#InventoryLotsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canUpdateSupplier = page.dataset.canUpdateSupplier === 'true';
    var canUpdateLotInfo = page.dataset.canUpdateLotInfo === 'true' || canUpdateSupplier;
    var $warehouseId = $('#InventoryLotsWarehouseId');
    var $stockItemId = $('#InventoryLotsStockItemId');
    var $lotNo = $('#InventoryLotsLotNo');
    var supplierModalElement = document.getElementById('InventoryLotSupplierModal');
    var supplierModal = supplierModalElement && window.bootstrap
        ? new bootstrap.Modal(supplierModalElement)
        : null;
    var unitCostModalElement = document.getElementById('InventoryLotUnitCostModal');
    var unitCostModal = unitCostModalElement && window.bootstrap
        ? new bootstrap.Modal(unitCostModalElement)
        : null;
    var $supplierForm = $('#InventoryLotSupplierForm');
    var $lotId = $('#InventoryLotSupplierLotId');
    var $lotLabel = $('#InventoryLotSupplierLotLabel');
    var $supplierId = $('#InventoryLotSupplierId');
    var $unitCostForm = $('#InventoryLotUnitCostForm');
    var $unitCostLotId = $('#InventoryLotUnitCostLotId');
    var $unitCostLotLabel = $('#InventoryLotUnitCostLotLabel');
    var $unitCost = $('#InventoryLotUnitCost');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function normalizeVndDigits(value) {
        return String(value || '').replace(/[^\d]/g, '');
    }

    function formatVndValue(value) {
        var digits = normalizeVndDigits(value);

        if (!digits) {
            return '';
        }

        return digits.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    function initializeVndMoneyInput(input) {
        input.value = formatVndValue(input.value);
        input.addEventListener('input', function () {
            var formatted = formatVndValue(input.value);

            if (input.value !== formatted) {
                input.value = formatted;
            }
        });
        input.addEventListener('blur', function () {
            input.value = formatVndValue(input.value);
        });
    }

    function formTokenHeaders($form) {
        var token = $form.find('input[name="__RequestVerificationToken"]').val();

        return token
            ? { RequestVerificationToken: token }
            : {};
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

    function showUnitCostModal() {
        if (unitCostModal) {
            unitCostModal.show();
            return;
        }

        $(unitCostModalElement).modal('show');
    }

    function hideUnitCostModal() {
        if (unitCostModal) {
            unitCostModal.hide();
            return;
        }

        $(unitCostModalElement).modal('hide');
    }

    function openSupplierModal(record) {
        $lotId.val(record.id);
        $lotLabel.text(record.lotNo + ' - ' + record.stockItem);
        $supplierId.val(record.supplierId || '');
        showSupplierModal();
    }

    function openUnitCostModal(record) {
        $unitCostLotId.val(record.id);
        $unitCostLotLabel.text(record.lotNo + ' - ' + record.stockItem);
        $unitCost.val(formatVndValue(record.unitCostValue || ''));
        showUnitCostModal();
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

    if (canUpdateLotInfo) {
        columnDefs.push({
            data: null,
            orderable: false,
            className: 'text-end text-nowrap',
            render: function () {
                return '<div class="dropdown">' +
                    '<button type="button" class="btn btn-outline-primary btn-sm" data-bs-toggle="dropdown" ' +
                    'aria-expanded="false" aria-label="' + encode(l('Actions')) + '">...</button>' +
                    '<div class="dropdown-menu dropdown-menu-end">' +
                    '<button type="button" class="dropdown-item" data-update-lot-supplier>' +
                    encode(l('Inventory:UpdateSupplierShort')) + '</button>' +
                    '<button type="button" class="dropdown-item" data-update-lot-unit-cost>' +
                    encode(l('Inventory:UpdateUnitCostShort')) + '</button>' +
                    '</div></div>';
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
                stockItemId: $stockItemId.val(),
                lotNo: $lotNo.val()
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

    $(tableSelector).on('click', '[data-update-lot-unit-cost]', function () {
        var record = dataTable.row($(this).closest('tr')).data();
        if (record) {
            openUnitCostModal(record);
        }
    });

    $supplierForm.on('submit', function (event) {
        event.preventDefault();

        if (!$supplierId.val()) {
            abp.notify.warn(l('Inventory:SupplierRequired'));
            return;
        }

        abp.ui.setBusy($supplierForm);
        abp.ajax({
            url: abp.appPath + 'Inventory/Lots?handler=UpdateSupplier&id=' +
                encodeURIComponent($lotId.val()) +
                '&supplierId=' +
                encodeURIComponent($supplierId.val()),
            type: 'POST',
            headers: formTokenHeaders($supplierForm)
        }).then(function () {
            abp.notify.success(l('Inventory:LotSupplierUpdatedSuccessfully'));
            hideSupplierModal();
            dataTable.ajax.reload(null, false);
        }).always(function () {
            abp.ui.clearBusy($supplierForm);
        });
    });

    $unitCostForm.on('submit', function (event) {
        event.preventDefault();

        var normalized = normalizeVndDigits($unitCost.val());
        var amount = Number(normalized);

        if (!normalized || !Number.isFinite(amount) || amount <= 0) {
            abp.notify.warn(l('Inventory:UnitCostPositive'));
            return;
        }

        abp.ui.setBusy($unitCostForm);
        abp.ajax({
            url: abp.appPath + 'Inventory/Lots?handler=UpdateUnitCost&id=' +
                encodeURIComponent($unitCostLotId.val()) +
                '&unitCost=' +
                encodeURIComponent(normalized),
            type: 'POST',
            headers: formTokenHeaders($unitCostForm)
        }).then(function () {
            abp.notify.success(l('Inventory:LotUnitCostUpdatedSuccessfully'));
            hideUnitCostModal();
            dataTable.ajax.reload(null, false);
        }).always(function () {
            abp.ui.clearBusy($unitCostForm);
        });
    });

    $('#InventoryLotsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    document.querySelectorAll('[data-vnd-money]').forEach(initializeVndMoneyInput);
})();
