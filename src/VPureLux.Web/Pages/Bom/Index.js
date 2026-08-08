(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-bom-index]');
    var tableSelector = '#BomProductsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    var canCreate = page.dataset.canCreate === 'true';
    var $searchTerm = $('#BomSearchTerm');
    var noCurrentVersionText = l('Bom:NoCurrentVersion');
    var versionNoText = l('Bom:VersionNo');
    var publishedText = l('Bom:Status:Published');
    var openHistoryText = l('Bom:OpenHistory');
    var createVersionText = l('Bom:CreateVersionForProduct');
    var viewCurrentVersionText = l('Bom:ViewCurrentVersion');
    var activeStatusText = l('Status:Active');
    var inactiveStatusText = l('Status:Inactive');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function productHtml(row) {
        return '<strong>' + encode(row.productCode) + '</strong>' +
            '<div class="text-muted">' + encode(row.productName) + '</div>';
    }

    function currentVersionHtml(row) {
        if (!row.currentVersion) {
            return '<span class="text-muted">' + encode(noCurrentVersionText) + '</span>';
        }

        var url = abp.appPath + 'Bom/Details/' + encodeURIComponent(row.currentVersion.id);
        return '<a href="' + url + '">' + encode(versionNoText) + ' ' +
            encode(row.currentVersion.versionNo) + '</a>' +
            '<span class="badge bg-success ms-1" data-bom-current-version>' +
            encode(publishedText) + '</span>';
    }

    function productStatusText(status) {
        return status === 'Active'
            ? activeStatusText
            : inactiveStatusText;
    }

    function actionsHtml(row) {
        var buttons = [
            '<a class="btn btn-sm btn-outline-secondary" href="' + abp.appPath + 'Bom/Product/' +
            encodeURIComponent(row.productId) + '">' + encode(openHistoryText) + '</a>'
        ];

        if (canCreate) {
            buttons.push('<a class="btn btn-sm btn-outline-primary" href="' + abp.appPath + 'Bom/Create/' +
                encodeURIComponent(row.productId) + '">' + encode(createVersionText) + '</a>');
        }

        if (row.currentVersion) {
            buttons.push('<a class="btn btn-sm btn-outline-secondary" href="' + abp.appPath + 'Bom/Details/' +
                encodeURIComponent(row.currentVersion.id) + '">' + encode(viewCurrentVersionText) + '</a>');
        }

        return buttons.join(' ');
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
                url: abp.appPath + 'Bom?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                keyword: $searchTerm.val()
            };
        }),
        columnDefs: [
            {
                data: null,
                render: function (_data, _type, row) {
                    return productHtml(row);
                }
            },
            {
                data: 'productStatus',
                render: function (data) {
                    return encode(productStatusText(data));
                }
            },
            {
                data: 'versionCount'
            },
            {
                data: null,
                orderable: false,
                render: function (_data, _type, row) {
                    return currentVersionHtml(row);
                }
            },
            {
                data: 'standardCostRange',
                orderable: false,
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                render: function (_data, _type, row) {
                    return actionsHtml(row);
                }
            }
        ]
    }));

    $('#BomSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#BomClearButton').on('click', function () {
        $searchTerm.val('');
        dataTable.ajax.reload();
    });
})();
