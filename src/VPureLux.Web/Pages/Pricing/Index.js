(function () {
    var localize = abp.localization.getResource('VPureLux');
    var page = document.querySelector('[data-pricing-index]');

    if (!page) {
        return;
    }

    var canViewProductHistory = page.dataset.canViewProductHistory === 'true';
    var canCreateProductSuggestedPrice = page.dataset.canCreateProductSuggestedPrice === 'true';
    var $productKeyword = $('#PricingProductsKeyword');
    var createNewVersionText = localize('Pricing:CreateNewVersion');
    var openHistoryText = localize('Pricing:OpenHistory');
    var publishedBomText = localize('Pricing:PublishedBom');
    var noPublishedBomText = localize('Pricing:NoPublishedBom');
    var noProductListPriceText = localize('Pricing:NoProductSuggestedPrice');

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
                data: null,
                orderable: false,
                className: 'text-start text-nowrap',
                render: function (_data, _type, row) {
                    return productActions(row);
                }
            },
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
                data: 'standardCostRange',
                className: 'text-end text-nowrap',
                render: function (data, _type, row) {
                    var className = row.hasPublishedBom && !row.hasMissingComponentSuggestedPrices
                        ? ''
                        : ' class="text-warning"';
                    return '<span' + className + '>' + encode(data || '-') + '</span>';
                }
            },
            {
                data: 'currentProductSuggestedPrice',
                className: 'text-end text-nowrap',
                render: function (data) {
                    return encode(formatMoney(data, 'VND') || noProductListPriceText);
                }
            }
        ]
    }));

    $('#PricingProductsSearchForm').on('submit', function (event) {
        event.preventDefault();
        productTable.ajax.reload();
    });

    $('#PricingProductsClearButton').on('click', function () {
        $productKeyword.val('');
        productTable.ajax.reload();
    });
})();
