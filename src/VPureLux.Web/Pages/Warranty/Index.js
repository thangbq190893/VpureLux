(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-warranty-index]');
    const tableSelector = '#WarrantyRemindersTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const canManageReminders = page.dataset.canManageReminders === 'true';
    const $searchText = $('#WarrantySearchText');
    const $status = $('#WarrantyStatus');
    const $dueFrom = $('#WarrantyDueFrom');
    const $dueTo = $('#WarrantyDueTo');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function tokenHeaders() {
        const token = $('#WarrantyTokenForm').find('input[name="__RequestVerificationToken"]').val();
        return token ? { RequestVerificationToken: token } : {};
    }

    function postAction(handler, row, data, successMessage) {
        return abp.ajax({
            url: abp.appPath + 'Warranty?handler=' + handler + '&id=' + encodeURIComponent(row.id),
            type: 'POST',
            headers: tokenHeaders(),
            data: data || {}
        }).then(function () {
            abp.notify.success(successMessage);
            dataTable.ajax.reload(null, false);
        });
    }

    function completeReminder(row) {
        abp.message.confirm(l('Warranty:ConfirmComplete'), l('Confirm')).then(function (confirmed) {
            if (confirmed) {
                postAction('Complete', row, null, l('Warranty:ReminderCompletedSuccessfully'));
            }
        });
    }

    function skipReminder(row) {
        abp.message.confirm(l('Warranty:ConfirmSkip'), l('Confirm')).then(function (confirmed) {
            if (confirmed) {
                postAction('Skip', row, null, l('Warranty:ReminderSkippedSuccessfully'));
            }
        });
    }

    function rescheduleReminder(row) {
        const dueDate = window.prompt(l('Warranty:NewDueDate'), row.dueDateIso || '');
        if (!dueDate) {
            return;
        }

        postAction('Reschedule', row, { dueDate: dueDate }, l('Warranty:ReminderRescheduledSuccessfully'));
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
                url: abp.appPath + 'Warranty?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: $searchText.val(),
                status: $status.val(),
                dueFrom: $dueFrom.val(),
                dueTo: $dueTo.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                orderable: false,
                className: 'text-start',
                render: function (_data, _type, row) {
                    if (!canManageReminders || !row.isPending) {
                        return '';
                    }

                    return '<div class="btn-group btn-group-sm" role="group">' +
                        '<button type="button" class="btn btn-outline-success js-warranty-complete">' + encode(l('Warranty:Complete')) + '</button>' +
                        '<button type="button" class="btn btn-outline-secondary js-warranty-reschedule">' + encode(l('Warranty:Reschedule')) + '</button>' +
                        '<button type="button" class="btn btn-outline-danger js-warranty-skip">' + encode(l('Warranty:Skip')) + '</button>' +
                        '</div>';
                }
            },
            { data: 'dueDate', className: 'text-nowrap', render: encode },
            {
                data: null,
                orderable: false,
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.statusBadgeClass) + '">' + encode(row.statusLabel) + '</span>';
                }
            },
            { data: 'customer', render: encode },
            { data: 'product', render: encode },
            { data: 'component', render: encode },
            { data: 'quantity', className: 'text-end text-nowrap', render: encode },
            { data: 'assetNo', className: 'text-nowrap', render: encode },
            { data: 'orderContext', className: 'text-nowrap', render: encode }
        ]
    }));

    $(tableSelector).on('click', '.js-warranty-complete', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            completeReminder(row);
        }
    });

    $(tableSelector).on('click', '.js-warranty-skip', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            skipReminder(row);
        }
    });

    $(tableSelector).on('click', '.js-warranty-reschedule', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            rescheduleReminder(row);
        }
    });

    $('#WarrantySearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#WarrantyClearButton').on('click', function () {
        $searchText.val('');
        $status.val('');
        $dueFrom.val('');
        $dueTo.val('');
        dataTable.ajax.reload();
    });
})();
