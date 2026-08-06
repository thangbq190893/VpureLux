(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-bom-index]');
    const tableSelector = '#BomProductsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const canCreate = page.dataset.canCreate === 'true';
    const $searchTerm = $('#BomSearchTerm');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function productHtml(row) {
        return '<strong>' + encode(row.productCode) + '</strong>' +
            '<div class="text-muted">' + encode(row.productName) + '</div>';
    }

    function currentVersionHtml(row) {
        if (!row.currentVersion) {
            return '<span class="text-muted">' + encode(l('Bom:NoCurrentVersion')) + '</span>';
        }

        const url = abp.appPath + 'Bom/Details/' + encodeURIComponent(row.currentVersion.id);
        return '<a href="' + url + '">' + encode(l('Bom:VersionNo')) + ' ' +
            encode(row.currentVersion.versionNo) + '</a>' +
            '<span class="badge bg-success ms-1" data-bom-current-version>' +
            encode(l('Bom:Status:Published')) + '</span>';
    }

    function actionsHtml(row) {
        const buttons = [
            '<a class="btn btn-sm btn-outline-secondary" href="' + abp.appPath + 'Bom/Product/' +
            encodeURIComponent(row.productId) + '">' + encode(l('Bom:OpenHistory')) + '</a>'
        ];

        if (canCreate) {
            buttons.push('<a class="btn btn-sm btn-outline-primary" href="' + abp.appPath + 'Bom/Create/' +
                encodeURIComponent(row.productId) + '">' + encode(l('Bom:CreateVersionForProduct')) + '</a>');
        }

        if (row.currentVersion) {
            buttons.push('<a class="btn btn-sm btn-outline-secondary" href="' + abp.appPath + 'Bom/Details/' +
                encodeURIComponent(row.currentVersion.id) + '">' + encode(l('Bom:ViewCurrentVersion')) + '</a>');
        }

        return buttons.join(' ');
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
                    return encode(l('Status:' + data));
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
