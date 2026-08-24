(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-machines]');
    const tableSelector = '#WarrantyMachinesTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const settingModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Warranty/MachineSettingModal' });
    const $searchText = $('#WarrantyMachineSearchText');
    const $isMachine = $('#WarrantyMachineEnabled');

    function encode(value) {
        return $('<div/>').text(value === null || value === undefined ? '' : value).html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
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
                url: abp.appPath + 'Warranty/Machines?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: $searchText.val(),
                isMachine: $isMachine.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                className: 'text-start',
                rowAction: {
                    items: [{
                        text: l('Warranty:ConfigureMachine'),
                        action: function (data) {
                            const record = recordOf(data);
                            settingModal.open({
                                productId: record.productId,
                                productCode: record.productCode,
                                productName: record.productName
                            });
                        }
                    }]
                }
            },
            { data: 'productCode', render: encode },
            { data: 'productName', render: encode },
            {
                data: 'productStatus',
                render: function (data) {
                    return encode(l('Status:' + data));
                }
            },
            {
                data: 'isMachine',
                render: function (data) {
                    return data ? encode(l('Yes')) : encode(l('No'));
                }
            },
            { data: 'note', render: encode }
        ]
    }));

    settingModal.onResult(function () {
        abp.notify.success(l('Warranty:MachineSettingSavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    });

    $('#WarrantyMachineSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#WarrantyMachineClearButton').on('click', function () {
        $searchText.val('');
        $isMachine.val('');
        dataTable.ajax.reload();
    });
})();
