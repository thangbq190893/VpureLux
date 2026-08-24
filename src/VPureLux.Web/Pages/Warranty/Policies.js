(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-policies]');
    const tableSelector = '#WarrantyPoliciesTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const policyModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Warranty/PolicyModal' });
    const $searchText = $('#WarrantyPolicySearchText');
    const $enabled = $('#WarrantyPolicyEnabled');

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
                url: abp.appPath + 'Warranty/Policies?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: $searchText.val(),
                isEnabled: $enabled.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                className: 'text-start',
                rowAction: {
                    items: [{
                        text: l('Warranty:EditPolicy'),
                        action: function (data) {
                            const record = recordOf(data);
                            policyModal.open({
                                componentId: record.componentId,
                                componentCode: record.componentCode,
                                componentName: record.componentName
                            });
                        }
                    }]
                }
            },
            { data: 'componentCode', render: encode },
            { data: 'componentName', render: encode },
            { data: 'componentUnit', render: encode },
            {
                data: 'isEnabled',
                render: function (data) {
                    return data ? encode(l('Yes')) : encode(l('No'));
                }
            },
            { data: 'cycleMonthsText', className: 'text-end', render: encode },
            { data: 'warningDaysBeforeDueText', className: 'text-end', render: encode },
            { data: 'note', render: encode }
        ]
    }));

    policyModal.onResult(function () {
        abp.notify.success(l('Warranty:PolicySavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    });

    $('#WarrantyPolicySearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#WarrantyPolicyClearButton').on('click', function () {
        $searchText.val('');
        $enabled.val('');
        dataTable.ajax.reload();
    });
})();
