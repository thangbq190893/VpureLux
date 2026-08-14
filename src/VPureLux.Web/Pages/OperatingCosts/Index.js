(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-operating-costs-index]');
    var tableSelector = '#OperatingCostsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canManageEntries = page.dataset.canManageEntries === 'true';
    var canDelete = page.dataset.canDelete === 'true';
    var $fromDate = $('#OperatingCostsFromDate');
    var $toDate = $('#OperatingCostsToDate');
    var $direction = $('#OperatingCostsDirection');
    var $paymentStatus = $('#OperatingCostsPaymentStatus');
    var $categoryId = $('#OperatingCostsCategoryId');
    var $searchText = $('#OperatingCostsSearchText');

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
            fromDate: window.vPureLuxDate ? window.vPureLuxDate.toIso($fromDate.val()) : $fromDate.val(),
            toDate: window.vPureLuxDate ? window.vPureLuxDate.toIso($toDate.val()) : $toDate.val(),
            direction: $direction.val(),
            paymentStatus: $paymentStatus.val(),
            categoryId: $categoryId.val(),
            searchText: $searchText.val()
        };
    }

    function refreshSummary() {
        abp.ajax({
            url: abp.appPath + 'OperatingCosts?handler=Summary',
            type: 'GET',
            data: getFilters()
        }).then(function (summary) {
            $('#OperatingCostsTotalIncome').text(summary.totalIncome);
            $('#OperatingCostsTotalExpense').text(summary.totalExpense);
            $('#OperatingCostsNetAmount').text(summary.netAmount);
            $('#OperatingCostsUnpaidReceivable').text(summary.unpaidReceivable);
            $('#OperatingCostsUnpaidPayable').text(summary.unpaidPayable);
        });
    }

    function deleteEntry(record) {
        abp.message.confirm(l('OperatingCosts:ConfirmDeleteEntry'), l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'OperatingCosts?handler=Delete&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(l('OperatingCosts:EntryDeletedSuccessfully'));
                dataTable.ajax.reload(null, false);
                refreshSummary();
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
            emptyTable: l('OperatingCosts:NoEntries'),
            zeroRecords: l('OperatingCosts:NoEntries')
        },
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'OperatingCosts?handler=List',
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
                                return canManageEntries;
                            },
                            action: function (data) {
                                window.location.href = abp.appPath + 'OperatingCosts/Edit?id=' + encodeURIComponent(recordOf(data).id);
                            }
                        },
                        {
                            text: l('Delete'),
                            visible: function () {
                                return canDelete;
                            },
                            action: function (data) {
                                deleteEntry(recordOf(data));
                            }
                        }
                    ]
                }
            },
            { data: 'entryDate', className: 'text-nowrap', render: encode },
            { data: 'direction', render: encode },
            { data: 'category', render: encode },
            {
                data: 'description',
                render: function (data) {
                    return '<span class="operating-costs-description-cell">' + encode(data) + '</span>';
                }
            },
            { data: 'amount', className: 'text-end text-nowrap', render: encode },
            {
                data: null,
                className: 'text-nowrap',
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.paymentStatusBadgeClass) + '">' +
                        encode(row.paymentStatus) + '</span>';
                }
            },
            {
                data: 'counterparty',
                render: function (data) {
                    return '<span class="operating-costs-counterparty-cell">' + encode(data) + '</span>';
                }
            }
        ]
    }));

    $(tableSelector).on('draw.dt', refreshSummary);
    refreshSummary();

    $('#OperatingCostsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#OperatingCostsClearButton').on('click', function () {
        $fromDate.val('');
        $toDate.val('');
        $direction.val('');
        $paymentStatus.val('');
        $categoryId.val('');
        $searchText.val('');
        dataTable.ajax.reload();
    });
})();
