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
    const $timingStatus = $('#WarrantyTimingStatus');
    const $dueFrom = $('#WarrantyDueFrom');
    const $dueTo = $('#WarrantyDueTo');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    const actionModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Warranty/ReminderActionModal' });
    actionModal.onResult(function () { dataTable.ajax.reload(null, false); });
    function openAction(row, action) {
        actionModal.open({ id: row.id, assetId: row.customerAssetId, action: action, dueDate: row.dueDateIso });
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
                timingStatus: $timingStatus.val(),
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
                        '<button type="button" class="btn btn-outline-dark js-warranty-suspend">' + encode(l('Warranty:Suspend')) + '</button>' +
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
            openAction(row, 'Complete');
        }
    });

    $(tableSelector).on('click', '.js-warranty-skip', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            openAction(row, 'Skip');
        }
    });

    $(tableSelector).on('click', '.js-warranty-reschedule', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) {
            openAction(row, 'Reschedule');
        }
    });

    $(tableSelector).on('click', '.js-warranty-suspend', function () {
        const row = dataTable.row($(this).closest('tr')).data();
        if (row) openAction(row, 'Suspend');
    });

    $('#WarrantySearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#WarrantyClearButton').on('click', function () {
        $searchText.val('');
        $status.val('');
        $timingStatus.val('');
        $dueFrom.val('');
        $dueTo.val('');
        dataTable.ajax.reload();
    });
})();
