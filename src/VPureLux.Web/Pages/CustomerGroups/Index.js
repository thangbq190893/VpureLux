(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-customer-groups-index]');
    const tableSelector = '#CustomerGroupsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/CreateModal' });
    const editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/EditModal' });
    const detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/DetailsModal' });
    const canEdit = page.dataset.canEdit === 'true';
    const canManageStatus = page.dataset.canManageStatus === 'true';
    const $searchText = $('#CustomerGroupsSearchText');
    const $status = $('#CustomerGroupsStatus');

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data?.record || data || {};
    }

    function reloadAfterModal() {
        abp.notify.success(l('CustomerGroups:SavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    }

    function postStatus(record, handler, confirmMessage, successMessageKey) {
        abp.message.confirm(confirmMessage, l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'CustomerGroups?handler=' + handler + '&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(l(successMessageKey));
                dataTable.ajax.reload(null, false);
            });
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
                url: abp.appPath + 'CustomerGroups?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: $searchText.val(),
                status: $status.val()
            };
        }),
        columnDefs: [
            {
                data: 'code',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'name',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'status',
                render: function (data) {
                    return encode(l('Status:' + data));
                }
            },
            {
                data: 'sortOrder'
            },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                rowAction: {
                    items: [
                        {
                            text: l('Details'),
                            action: function (data) {
                                detailsModal.open({ id: recordOf(data).id });
                            }
                        },
                        {
                            text: l('Edit'),
                            visible: function () {
                                return canEdit;
                            },
                            action: function (data) {
                                editModal.open({ id: recordOf(data).id });
                            }
                        },
                        {
                            text: l('Deactivate'),
                            visible: function (data) {
                                return canManageStatus && recordOf(data).status === 'Active';
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Deactivate',
                                    l('CustomerGroups:ConfirmDeactivate'),
                                    'CustomerGroups:DeactivatedSuccessfully');
                            }
                        },
                        {
                            text: l('Activate'),
                            visible: function (data) {
                                return canManageStatus && recordOf(data).status !== 'Active';
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Activate',
                                    l('CustomerGroups:ConfirmActivate'),
                                    'CustomerGroups:ActivatedSuccessfully');
                            }
                        }
                    ]
                }
            }
        ]
    }));

    createModal.onResult(reloadAfterModal);
    editModal.onResult(reloadAfterModal);

    document.querySelector('[data-customer-group-create]')?.addEventListener('click', function (event) {
        event.preventDefault();
        createModal.open();
    });

    $('#CustomerGroupsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#CustomerGroupsStatus').on('change', function () {
        dataTable.ajax.reload();
    });

    $('#CustomerGroupsClearButton').on('click', function () {
        $searchText.val('');
        $status.val('');
        dataTable.ajax.reload();
    });
})();
