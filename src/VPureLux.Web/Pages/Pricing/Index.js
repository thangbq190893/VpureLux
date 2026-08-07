(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-pricing-index]');

    if (!page) {
        return;
    }

    const canViewComponentHistory = page.dataset.canViewComponentHistory === 'true';
    const canViewProductHistory = page.dataset.canViewProductHistory === 'true';
    const $componentKeyword = $('#PricingComponentsKeyword');
    const $productKeyword = $('#PricingProductsKeyword');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    function formatMoney(value, currency) {
        if (value === null || value === undefined) {
            return null;
        }

        return new Intl.NumberFormat('vi-VN', {
            maximumFractionDigits: 0
        }).format(value) + ' ' + (currency || 'VND');
    }

    function actionLink(url, visible) {
        if (!visible) {
            return '';
        }

        return '<a class="btn btn-sm btn-outline-secondary" href="' + url + '">' +
            encode(l('Pricing:OpenHistory')) + '</a>';
    }

    const componentTable = $('#PricingComponentsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Pricing?handler=ComponentList',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                keyword: $componentKeyword.val()
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
                data: null,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    const price = formatMoney(row.currentSuggestedSellingPrice, row.currency);
                    return price === null
                        ? '<span class="text-muted">' + encode(l('Pricing:NoComponentSuggestedPrice')) + '</span>'
                        : encode(price);
                }
            },
            {
                data: 'effectiveFrom',
                className: 'text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    return actionLink(
                        abp.appPath + 'Pricing/Components/' + encodeURIComponent(row.componentId),
                        canViewComponentHistory);
                }
            }
        ]
    }));

    const productTable = $('#PricingProductsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Pricing?handler=ProductList',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                keyword: $productKeyword.val()
            };
        }),
        columnDefs: [
            {
                data: 'productCode',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'productName',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'hasPublishedBom',
                render: function (data) {
                    return data
                        ? '<span class="badge bg-success">' + encode(l('Pricing:PublishedBom')) + '</span>'
                        : '<span class="badge bg-warning text-dark">' + encode(l('Pricing:NoPublishedBom')) + '</span>';
                }
            },
            {
                data: null,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    if (!row.hasPublishedBom) {
                        return '<span class="text-muted">' + encode(l('Pricing:NoPublishedBom')) + '</span>';
                    }

                    if (row.hasMissingComponentSuggestedPrices) {
                        return '<span class="text-warning">' + encode(l('Pricing:MissingComponentPrices')) + '</span>';
                    }

                    return encode(formatMoney(row.componentBuildPrice, 'VND') || '-');
                }
            },
            {
                data: 'currentProductSuggestedPrice',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(formatMoney(data, 'VND') || l('Pricing:NoProductSuggestedPrice'));
                }
            },
            {
                data: 'difference',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(formatMoney(data, 'VND') || '-');
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    return actionLink(
                        abp.appPath + 'Pricing/Products/' + encodeURIComponent(row.productId),
                        canViewProductHistory);
                }
            }
        ]
    }));

    $('#PricingComponentsSearchForm').on('submit', function (event) {
        event.preventDefault();
        componentTable.ajax.reload();
    });

    $('#PricingComponentsClearButton').on('click', function () {
        $componentKeyword.val('');
        componentTable.ajax.reload();
    });

    $('#PricingProductsSearchForm').on('submit', function (event) {
        event.preventDefault();
        productTable.ajax.reload();
    });

    $('#PricingProductsClearButton').on('click', function () {
        $productKeyword.val('');
        productTable.ajax.reload();
    });
})();
