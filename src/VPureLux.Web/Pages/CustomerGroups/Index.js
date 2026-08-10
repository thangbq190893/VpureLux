(function () {
    var localize = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-customer-groups-index]');
    var tableSelector = '#CustomerGroupsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var createModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/CreateModal' });
    var editModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/EditModal' });
    var detailsModal = new abp.ModalManager({ viewUrl: abp.appPath + 'CustomerGroups/DetailsModal' });
    var canEdit = page.dataset.canEdit === 'true';
    var canManageStatus = page.dataset.canManageStatus === 'true';
    var $searchText = $('#CustomerGroupsSearchText');
    var $status = $('#CustomerGroupsStatus');

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function statusKey(value) {
        if (value === 1 || value === '1' || value === 'Active') {
            return 'Active';
        }

        if (value === 2 || value === '2' || value === 'Inactive') {
            return 'Inactive';
        }

        return value || '';
    }

    function statusText(value) {
        var key = statusKey(value);
        return key ? localize('Status:' + key) : '';
    }

    function isActiveStatus(value) {
        return statusKey(value) === 'Active';
    }

    function reloadAfterModal() {
        abp.notify.success(localize('CustomerGroups:SavedSuccessfully'));
        dataTable.ajax.reload(null, false);
    }

    function postStatus(record, handler, confirmMessage, successMessageKey) {
        abp.message.confirm(confirmMessage, localize('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'CustomerGroups?handler=' + handler + '&id=' + encodeURIComponent(record.id),
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
                data: null,
                orderable: false,
                className: 'text-start',
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
                                return canManageStatus && isActiveStatus(recordOf(data).status);
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Deactivate',
                                    localize('CustomerGroups:ConfirmDeactivate'),
                                    'CustomerGroups:DeactivatedSuccessfully');
                            }
                        },
                        {
                            text: localize('Activate'),
                            visible: function (data) {
                                return canManageStatus && !isActiveStatus(recordOf(data).status);
                            },
                            action: function (data) {
                                postStatus(
                                    recordOf(data),
                                    'Activate',
                                    localize('CustomerGroups:ConfirmActivate'),
                                    'CustomerGroups:ActivatedSuccessfully');
                            }
                        }
                    ]
                }
            },
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
                data: 'sortOrder'
            },
            {
                data: 'status',
                render: function (data) {
                    return encode(statusText(data));
                }
            }
        ]
    }));

    createModal.onResult(reloadAfterModal);
    editModal.onResult(reloadAfterModal);

    var createButton = document.querySelector('[data-customer-group-create]');
    if (createButton) {
        createButton.addEventListener('click', function (event) {
            event.preventDefault();
            createModal.open();
        });
    }

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
