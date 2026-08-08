(function () {
    var l = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-bom-index]');
    var tableSelector = '#BomProductsTable';
    var editCurrentForm = document.querySelector('[data-bom-edit-current-form]');
    var editCurrentId = document.getElementById('BomEditCurrentId');

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
    var editCurrentVersionText = l('Bom:EditCurrentVersion');
    var editShortText = l('Edit');
    var openHistoryShortText = l('Bom:OpenHistoryShort');
    var createVersionShortText = l('Bom:CreateVersionShort');
    var viewCurrentVersionShortText = l('Bom:ViewCurrentVersionShort');
    var versionCountSuffixText = l('Bom:VersionCountSuffix');
    var activeStatusText = l('Status:Active');
    var inactiveStatusText = l('Status:Inactive');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function compactText(value, maxLength) {
        var text = value || '';
        var breakIndex;
        var compacted;

        if (text.length <= maxLength) {
            return text;
        }

        compacted = text.substring(0, maxLength + 1);
        breakIndex = compacted.lastIndexOf(' ');
        if (breakIndex > 40) {
            compacted = compacted.substring(0, breakIndex);
        } else {
            compacted = compacted.substring(0, maxLength);
        }

        return compacted.replace(/\s+$/, '') + '...';
    }

    function productHtml(row) {
        var productName = row.productName || '';
        return '<div class="bom-product-cell">' +
            '<strong class="bom-product-code">' + encode(row.productCode) + '</strong>' +
            '<span class="text-muted bom-product-name" title="' + encode(productName) + '">' +
            encode(compactText(productName, 76)) + '</span>' +
            '</div>';
    }

    function currentVersionHtml(row) {
        if (!row.currentVersion) {
            return '<span class="text-muted">' + encode(noCurrentVersionText) + '</span>';
        }

        var url = abp.appPath + 'Bom/Details/' + encodeURIComponent(row.currentVersion.id);
        return '<span class="bom-current-version-cell"><a href="' + url + '">' + encode(versionNoText) + ' ' +
            encode(row.currentVersion.versionNo) + '</a>' +
            '<span class="badge bg-success ms-1" data-bom-current-version>' +
            encode(publishedText) + '</span></span>';
    }

    function bomStateHtml(row) {
        return '<div class="bom-state-cell">' +
            '<span class="bom-state-line">' + encode(productStatusText(row.productStatus)) + '</span>' +
            '<span class="bom-state-line bom-version-count">' + encode(row.versionCount) + ' ' +
            encode(versionCountSuffixText) + '</span>' +
            '<span class="bom-state-line">' + currentVersionHtml(row) + '</span>' +
            '</div>';
    }

    function productStatusText(status) {
        return status === 'Active'
            ? activeStatusText
            : inactiveStatusText;
    }

    function actionsHtml(row) {
        var buttons = [
            '<a class="btn btn-sm btn-outline-secondary" title="' + encode(openHistoryText) +
            '" href="' + abp.appPath + 'Bom/Product/' +
            encodeURIComponent(row.productId) + '">' + encode(openHistoryShortText) + '</a>'
        ];

        if (canCreate && row.currentVersion) {
            buttons.push('<button type="button" class="btn btn-sm btn-outline-primary" title="' +
                encode(editCurrentVersionText) + '" data-bom-edit-current-id="' +
                encode(row.currentVersion.id) + '">' + encode(editShortText) + '</button>');
        }

        if (canCreate) {
            buttons.push('<a class="btn btn-sm btn-outline-primary" title="' + encode(createVersionText) +
                '" href="' + abp.appPath + 'Bom/Create/' +
                encodeURIComponent(row.productId) + '">' + encode(createVersionShortText) + '</a>');
        }

        if (row.currentVersion) {
            buttons.push('<a class="btn btn-sm btn-outline-secondary" title="' + encode(viewCurrentVersionText) +
                '" href="' + abp.appPath + 'Bom/Details/' +
                encodeURIComponent(row.currentVersion.id) + '">' + encode(viewCurrentVersionShortText) + '</a>');
        }

        return '<div class="bom-action-group">' + buttons.join('') + '</div>';
    }

    function submitEditCurrent(versionId) {
        if (!editCurrentForm || !editCurrentId || !versionId) {
            abp.notify.error(page.dataset.editCurrentError);
            return;
        }

        abp.message.confirm(page.dataset.confirmEditCurrent, page.dataset.confirmTitle).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            editCurrentId.value = versionId;
            abp.ui.setBusy(editCurrentForm);
            editCurrentForm.submit();
        }).catch(function () {
            abp.ui.clearBusy(editCurrentForm);
            abp.notify.error(page.dataset.editCurrentError);
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
                className: 'bom-product-column',
                width: '40%',
                render: function (_data, _type, row) {
                    return productHtml(row);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'bom-state-column',
                width: '20%',
                render: function (_data, _type, row) {
                    return bomStateHtml(row);
                }
            },
            {
                data: 'standardCostRange',
                orderable: false,
                className: 'bom-standard-cost-column',
                width: '25%',
                render: function (data) {
                    return '<span class="bom-standard-cost-cell">' + encode(data) + '</span>';
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end bom-actions-column',
                width: '15%',
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

    $(tableSelector).on('click', '[data-bom-edit-current-id]', function () {
        submitEditCurrent(this.getAttribute('data-bom-edit-current-id'));
    });
})();
