(function () {
    var localize = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-customers-index]');
    var tableSelector = '#CustomersTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/CreateModal' });
    var editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/EditModal' });
    var detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'Customers/DetailsModal' });
    var canEdit = page.dataset.canEdit === 'true';
    var canManageStatus = page.dataset.canManageStatus === 'true';
    var $searchText = $('#CustomersSearchText');
    var $status = $('#CustomersStatus');

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function reloadAfterModal() {
        abp.notify.success(localize('Customers:SavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    }

    function postStatus(record, handler, confirmMessage, successMessageKey) {
        abp.message.confirm(confirmMessage, localize('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Customers?handler=' + handler + '&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(localize(successMessageKey));
                dataTable.ajax.reload(null, false);
            });
        });
    }

    var dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
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
                    return encode(localize('Status:' + data));
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                rowAction: {
                    items: [
                        {
                            text: localize('Details'),
                            action: function (data) {
                                detailsModal.open({ id: recordOf(data).id });
                            }
                        },
                        {
                            text: localize('Edit'),
                            visible: function () {
                                return canEdit;
                            },
                            action: function (data) {
                                editModal.open({ id: recordOf(data).id });
                            }
                        },
                        {
                            text: localize('Deactivate'),
                            visible: function (data) {
                                return canManageStatus && recordOf(data).status === 'Active';
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Deactivate',
                                    localize('Customers:ConfirmDeactivate'),
                                    'Customers:DeactivatedSuccessfully');
                            }
                        },
                        {
                            text: localize('Activate'),
                            visible: function (data) {
                                return canManageStatus && recordOf(data).status !== 'Active';
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Activate',
                                    localize('Customers:ConfirmActivate'),
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

    var createButton = document.querySelector('[data-customer-create]');
    if (createButton) {
        createButton.addEventListener('click', function (event) {
            event.preventDefault();
            createModal.open();
        });
    }

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
