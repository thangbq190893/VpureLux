(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-catalog-index]');
    const tableSelector = '#ProductsTable';

    if (!page || !document.querySelector(tableSelector)) {
        return;
    }

    const canEdit = page.dataset.canEdit === 'true';
    const canViewPricingContext = page.dataset.canViewPricingContext === 'true';
    const $keyword = $('#ProductsKeyword');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function recordOf(data) {
        return data && data.record ? data.record : (data || {});
    }

    function formatMoney(value) {
        if (value === null || value === undefined) {
            return l('Catalog:NoProductSuggestedPrice');
        }

        return new Intl.NumberFormat('vi-VN', {
            maximumFractionDigits: 0
        }).format(value) + ' ₫';
    }

    function imageHtml(record) {
        if (!record.hasImage) {
            return '<div class="vpl-catalog-thumbnail vpl-catalog-thumbnail-placeholder border rounded d-flex align-items-center justify-content-center text-muted" title="' +
                encode(l('Catalog:NoImage')) + '">-</div>';
        }

        const imageHash = record.imageHash ? '?v=' + encodeURIComponent(record.imageHash) : '';
        return '<img src="' + abp.appPath + 'api/catalog/products/' + record.id + '/thumbnail' + imageHash +
            '" loading="lazy" width="40" height="40" class="vpl-catalog-thumbnail border rounded" alt="' +
            encode(record.name) + '" />';
    }

    function bomStatusHtml(record) {
        if (record.hasPublishedBom) {
            return '<span class="badge bg-success">' + encode(l('Catalog:PublishedBomAvailable')) + '</span>';
        }

        return '<span class="badge bg-warning text-dark">' + encode(l('Catalog:NoPublishedBom')) + '</span>';
    }

    function postStatus(record, handler, confirmMessage, successMessage) {
        abp.message.confirm(confirmMessage, l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ajax({
                url: abp.appPath + 'Catalog/Products?handler=' + handler + '&id=' + encodeURIComponent(record.id),
                type: 'POST'
            }).then(function () {
                abp.notify.success(successMessage);
                dataTable.ajax.reload(null, false);
            });
        });
    }

    const columnDefs = [
        {
            data: null,
            orderable: false,
            className: 'vpl-catalog-thumbnail-cell',
            render: function (_data, _type, row) {
                return imageHtml(row);
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
            data: 'status',
            render: function (data) {
                return encode(l('Status:' + data));
            }
        }
    ];

    if (canViewPricingContext) {
        columnDefs.push(
            {
                data: 'currentProductSuggestedPrice',
                orderable: false,
                className: 'text-end',
                render: function (data) {
                    return encode(formatMoney(data));
                }
            },
            {
                data: null,
                orderable: false,
                render: function (_data, _type, row) {
                    return bomStatusHtml(row);
                }
            });
    }

    columnDefs.push({
        data: null,
        orderable: false,
        className: 'text-end',
        rowAction: {
            items: [
                {
                    text: l('Details'),
                    action: function (data) {
                        const record = recordOf(data);
                        page.catalogModals.details.open({ id: record.id });
                    }
                },
                {
                    text: l('Edit'),
                    visible: function () {
                        return canEdit;
                    },
                    action: function (data) {
                        const record = recordOf(data);
                        page.catalogModals.edit.open({ id: record.id });
                    }
                },
                {
                    text: l('Catalog:ManageImage'),
                    visible: function () {
                        return canEdit;
                    },
                    action: function (data) {
                        const record = recordOf(data);
                        location.href = abp.appPath + 'Catalog/Products/Edit/' + record.id;
                    }
                },
                {
                    text: l('Deactivate'),
                    visible: function (data) {
                        const record = recordOf(data);
                        return canEdit && record.status === 'Active';
                    },
                    action: function (data) {
                        const record = recordOf(data);
                        postStatus(
                            record,
                            'Deactivate',
                            l('Catalog:ConfirmDeactivateProduct'),
                            page.dataset.deactivatedMessage);
                    }
                },
                {
                    text: l('Activate'),
                    visible: function (data) {
                        const record = recordOf(data);
                        return canEdit && record.status !== 'Active';
                    },
                    action: function (data) {
                        const record = recordOf(data);
                        postStatus(
                            record,
                            'Activate',
                            l('Catalog:ConfirmActivateProduct'),
                            page.dataset.activatedMessage);
                    }
                }
            ]
        }
    });

    const dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Catalog/Products?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                keyword: $keyword.val()
            };
        }),
        columnDefs: columnDefs
    }));

    $('#ProductsSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#ProductsClearButton').on('click', function () {
        $keyword.val('');
        dataTable.ajax.reload();
    });
})();
