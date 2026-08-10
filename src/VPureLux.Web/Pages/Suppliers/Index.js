(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-suppliers-index]');
    var tableSelector = '#SuppliersTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canEdit = page.dataset.canEdit === 'true';
    var canDelete = page.dataset.canDelete === 'true';
    var searchText = $('#SuppliersSearchText');

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function deleteSupplier(record) {
        abp.message.confirm(l('Suppliers:ConfirmDelete'), l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Suppliers?handler=Delete&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(l('Suppliers:DeletedSuccessfully'));
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
        language: {
            emptyTable: l('Suppliers:NoSuppliers'),
            zeroRecords: l('Suppliers:NoSuppliers')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Suppliers?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                searchText: searchText.val()
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
                            text: l('Edit'),
                            visible: function () {
                                return canEdit;
                            },
                            action: function (data) {
                                window.location.href = abp.appPath + 'Suppliers/Edit?id=' + encodeURIComponent(recordOf(data).id);
                            }
                        },
                        {
                            text: l('Delete'),
                            visible: function () {
                                return canDelete;
                            },
                            action: function (data) {
                                deleteSupplier(recordOf(data));
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
                render: function (data, type, row) {
                    var html = '<strong>' + encode(data) + '</strong>';
                    if (row.contactName) {
                        html += '<div class="text-muted small">' + encode(row.contactName) + '</div>';
                    }

                    return html;
                }
            },
            {
                data: 'phone',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'taxCode',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'address',
                render: function (data) {
                    return encode(data);
                }
            }
        ]
    }));

    $('#SuppliersSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#SuppliersClearButton').on('click', function () {
        searchText.val('');
        dataTable.ajax.reload();
    });
})();
