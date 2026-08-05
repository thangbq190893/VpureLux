(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-customers-index]');
    const tableSelector = '#CustomersTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/CreateModal' });
    const editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/EditModal' });
    const detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/DetailsModal' });
    const canEdit = page.dataset.canEdit === 'true';
    const canManageStatus = page.dataset.canManageStatus === 'true';
    const $searchText = $('#CustomersSearchText');
    const $status = $('#CustomersStatus');

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
        abp.notify.success(l('Customers:SavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    }

    function postStatus(record, handler, confirmMessage, successMessageKey) {
        abp.message.confirm(confirmMessage, l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Customers?handler=' + handler + '&id=' + encodeURIComponent(record.id),
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
                url: abp.appPath + 'Customers?handler=List',
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
                data: 'customerGroupName',
                orderable: false,
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
                                    l('Customers:ConfirmDeactivate'),
                                    'Customers:DeactivatedSuccessfully');
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
                                    l('Customers:ConfirmActivate'),
                                    'Customers:ActivatedSuccessfully');
                            }
                        }
                    ]
                }
            }
        ]
    }));

    createModal.onResult(reloadAfterModal);
    editModal.onResult(reloadAfterModal);

    document.querySelector('[data-customer-create]')?.addEventListener('click', function (event) {
        event.preventDefault();
        createModal.open();
    });

    $('#CustomersSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#CustomersStatus').on('change', function () {
        dataTable.ajax.reload();
    });

    $('#CustomersClearButton').on('click', function () {
        $searchText.val('');
        $status.val('');
        dataTable.ajax.reload();
    });
})();
