(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-policies]');
    const tableSelector = '#WarrantyPoliciesTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const $searchText = $('#WarrantyPolicySearchText');
    const $enabled = $('#WarrantyPolicyEnabled');

    function encode(value) {
        return $('<div/>').text(value === null || value === undefined ? '' : value).html();
    }

    function tokenHeaders() {
        const token = $('#WarrantyPolicyTokenForm').find('input[name="__RequestVerificationToken"]').val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function savePolicy(row) {
        const defaultCycle = row.cycleMonths || 3;
        const cycleMonths = window.prompt(l('Warranty:CycleMonths'), defaultCycle);
        if (!cycleMonths) {
            return;
        }

        const defaultWarning = row.warningDaysBeforeDue === null || row.warningDaysBeforeDue === undefined
            ? 7
            : row.warningDaysBeforeDue;
        const warningDaysBeforeDue = window.prompt(l('Warranty:WarningDaysBeforeDue'), defaultWarning);
        if (warningDaysBeforeDue === null) {
            return;
        }

        abp.ajax({
            url: abp.appPath + 'Warranty/Policies?handler=Save',
            type: 'POST',
            headers: tokenHeaders(),
            data: {
                componentId: row.componentId,
                isEnabled: true,
                cycleMonths: cycleMonths,
                warningDaysBeforeDue: warningDaysBeforeDue,
                note: row.note || ''
            }
        }).then(function () {
            abp.notify.success(l('Warranty:PolicySavedSuccessfully'));
            dataTable.ajax.reload(null, false);
        });
    }

    function disablePolicy(row) {
        abp.ajax({
            url: abp.appPath + 'Warranty/Policies?handler=Save',
            type: 'POST',
            headers: tokenHeaders(),
            data: {
                componentId: row.componentId,
                isEnabled: false,
                cycleMonths: row.cycleMonths || 3,
                warningDaysBeforeDue: row.warningDaysBeforeDue || 7,
                note: row.note || ''
            }
        }).then(function () {
            abp.notify.success(l('Warranty:PolicySavedSuccessfully'));
            dataTable.ajax.reload(null, false);
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
                render: function (_data, _type, row) {
                    let html = '<div class="btn-group btn-group-sm" role="group">' +
                        '<button type="button" class="btn btn-outline-primary js-warranty-policy-save">' +
                        encode(l('Warranty:EditPolicy')) + '</button>';

                    if (row.isEnabled) {
                        html += '<button type="button" class="btn btn-outline-secondary js-warranty-policy-disable">' +
                            encode(l('Deactivate')) + '</button>';
                    }

                    return html + '</div>';
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

    $(tableSelector).on('click', '.js-warranty-policy-save', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            savePolicy(row);
        }
    });

    $(tableSelector).on('click', '.js-warranty-policy-disable', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            disablePolicy(row);
        }
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
