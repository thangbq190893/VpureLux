(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-operating-cost-categories]');
    var tableSelector = '#OperatingCostCategoriesTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canManage = page.dataset.canManage === 'true';
    var $searchText = $('#OperatingCostCategoriesSearchText');
    var $direction = $('#OperatingCostCategoriesDirection');
    var $isActive = $('#OperatingCostCategoriesIsActive');

    if (page.dataset.statusSuccess) {
        abp.notify.success(page.dataset.statusSuccess);
    }

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function getFilters() {
        return {
            searchText: $searchText.val(),
            direction: $direction.val(),
            isActive: $isActive.val()
        };
    }

    function deleteCategory(record) {
        abp.message.confirm(l('OperatingCosts:ConfirmDeleteCategory'), l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'OperatingCosts/Categories?handler=Delete&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(l('OperatingCosts:CategoryDeletedSuccessfully'));
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
            emptyTable: l('OperatingCosts:NoCategories'),
            zeroRecords: l('OperatingCosts:NoCategories')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'OperatingCosts/Categories?handler=List',
                type: 'GET',
                data: input
            });
        }, getFilters),
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
                                return canManage;
                            },
                            action: function (data) {
                                window.location.href = abp.appPath + 'OperatingCosts/EditCategory?id=' + encodeURIComponent(recordOf(data).id);
                            }
                        },
                        {
                            text: l('Delete'),
                            visible: function () {
                                return canManage;
                            },
                            action: function (data) {
                                deleteCategory(recordOf(data));
                            }
                        }
                    ]
                }
            },
            { data: 'code', render: encode },
            { data: 'name', render: encode },
            { data: 'direction', render: encode },
            {
                data: null,
                className: 'text-nowrap',
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.statusBadgeClass) + '">' + encode(row.status) + '</span>';
                }
            }
        ]
    }));

    $('#OperatingCostCategoriesSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#OperatingCostCategoriesClearButton').on('click', function () {
        $searchText.val('');
        $direction.val('');
        $isActive.val('');
        dataTable.ajax.reload();
    });
})();
