(function () {
    var localize = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-pricing-index]');

    if (!page) {
        return;
    }

    var canViewComponentHistory = page.dataset.canViewComponentHistory === 'true';
    var canViewProductHistory = page.dataset.canViewProductHistory === 'true';
    var canCreateComponentSuggestedPrice = page.dataset.canCreateComponentSuggestedPrice === 'true';
    var canCreateProductSuggestedPrice = page.dataset.canCreateProductSuggestedPrice === 'true';
    var $componentKeyword = $('#PricingComponentsKeyword');
    var $productKeyword = $('#PricingProductsKeyword');
    var createNewVersionText = localize('Pricing:CreateNewVersion');
    var openHistoryText = localize('Pricing:OpenHistory');
    var noComponentSuggestedPriceText = localize('Pricing:NoComponentSuggestedPrice');
    var publishedBomText = localize('Pricing:PublishedBom');
    var noPublishedBomText = localize('Pricing:NoPublishedBom');
    var missingComponentPricesText = localize('Pricing:MissingComponentPrices');
    var noProductSuggestedPriceText = localize('Pricing:NoProductSuggestedPrice');

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

    function actionLink(url, visible, text, buttonClass) {
        if (!visible) {
            return '';
        }

        return '<a class="btn btn-sm ' + encode(buttonClass || 'btn-outline-secondary') + '" href="' + url + '">' +
            encode(text) + '</a>';
    }

    function componentActions(row) {
        var actions = [];

        if (canCreateComponentSuggestedPrice && row.canCreateSuggestedPrice) {
            actions.push(actionLink(
                abp.appPath + 'Pricing/Components/Create/' + encodeURIComponent(row.componentId),
                true,
                createNewVersionText,
                'btn-primary'));
        }

        var historyLink = actionLink(
            abp.appPath + 'Pricing/Components/' + encodeURIComponent(row.componentId),
            canViewComponentHistory,
            openHistoryText);

        if (historyLink) {
            actions.push(historyLink);
        }

        return renderActions(actions);
    }

    function productActions(row) {
        var actions = [];

        if (canCreateProductSuggestedPrice && row.canCreateSuggestedPrice) {
            actions.push(actionLink(
                abp.appPath + 'Pricing/Products/Create/' + encodeURIComponent(row.productId),
                true,
                createNewVersionText,
                'btn-primary'));
        }

        var historyLink = actionLink(
            abp.appPath + 'Pricing/Products/' + encodeURIComponent(row.productId),
            canViewProductHistory,
            openHistoryText);

        if (historyLink) {
            actions.push(historyLink);
        }

        return renderActions(actions);
    }

    function renderActions(actions) {
        return actions.length
            ? '<div class="d-inline-flex gap-1 justify-content-end">' + actions.join('') + '</div>'
            : '';
    }

    var componentTable = $('#PricingComponentsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
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
                    var price = formatMoney(row.currentSuggestedSellingPrice, row.currency);
                    return price === null
                        ? '<span class="text-muted">' + encode(noComponentSuggestedPriceText) + '</span>'
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
                    return componentActions(row);
                }
            }
        ]
    }));

    var productTable = $('#PricingProductsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
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
                        ? '<span class="badge bg-success">' + encode(publishedBomText) + '</span>'
                        : '<span class="badge bg-warning text-dark">' + encode(noPublishedBomText) + '</span>';
                }
            },
            {
                data: null,
                className: 'text-end text-nowrap',
                render: function (_data, _type, row) {
                    if (!row.hasPublishedBom) {
                        return '<span class="text-muted">' + encode(noPublishedBomText) + '</span>';
                    }

                    if (row.hasMissingComponentSuggestedPrices) {
                        return '<span class="text-warning">' + encode(missingComponentPricesText) + '</span>';
                    }

                    return encode(formatMoney(row.componentBuildPrice, 'VND') || '-');
                }
            },
            {
                data: 'currentProductSuggestedPrice',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(formatMoney(data, 'VND') || noProductSuggestedPriceText);
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
                    return productActions(row);
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
