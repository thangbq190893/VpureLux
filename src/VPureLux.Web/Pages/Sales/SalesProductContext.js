(function (window) {
    var page = document.getElementById('SalesCreatePage') || document.getElementById('SalesEditPage');
    if (!page) {
        return;
    }

    var l = abp.localization.getResource('VPureLux');
    var defaultContextHtml = null;

    function appendProductId(url, productId) {
        return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'productId=' + encodeURIComponent(productId);
    }

    function getValue(data, key) {
        var camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        return data[key] ?? data[camelKey];
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function captureDefaultContextHtml() {
        if (defaultContextHtml !== null) {
            return defaultContextHtml;
        }

        var templatePanel = document.getElementById('sales-line-row-template');
        var panel = templatePanel
            ? templatePanel.content.querySelector('[data-sales-product-context]')
            : page.querySelector('[data-sales-line-row] [data-sales-product-context]');

        defaultContextHtml = panel ? panel.innerHTML : l('Sales:SelectProductForContext');
        return defaultContextHtml;
    }

    function getProductSelector(scope) {
        return scope.querySelector('[data-sales-product-select]')
            || scope.querySelector('[data-sales-product-selector]');
    }

    function renderContext(scope, data) {
        var contextPanel = scope.querySelector('[data-sales-product-context]');
        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');

        if (!contextPanel) {
            return;
        }

        var hasPublishedBom = getValue(data, 'HasPublishedBom') === true || getValue(data, 'HasPublishedBom') === 'true';
        var hasImage = getValue(data, 'HasImage') === true || getValue(data, 'HasImage') === 'true';
        var suggestedPrice = getValue(data, 'SuggestedPrice');
        var bomBadgeClass = hasPublishedBom ? 'badge bg-success' : 'badge bg-warning text-dark';
        var bomText = hasPublishedBom ? l('Sales:PublishedBomAvailable') : l('Sales:NoPublishedBom');
        var imageText = hasImage ? l('Sales:HasProductImage') : l('Sales:NoProductImage');
        var suggestedPriceText = suggestedPrice === null || suggestedPrice === undefined
            ? l('Sales:NoSuggestedPrice')
            : suggestedPrice;

        contextPanel.innerHTML =
            '<div class="mb-2"><span class="' + bomBadgeClass + '">' + escapeHtml(bomText) + '</span></div>' +
            '<div class="small text-muted mb-1">' + escapeHtml(l('Sales:ProductImage')) + ': ' + escapeHtml(imageText) + '</div>' +
            '<div class="small">' + escapeHtml(l('Sales:SuggestedPrice')) + ': ' + escapeHtml(String(suggestedPriceText)) + '</div>';

        if (actualPriceInput && !actualPriceInput.value && suggestedPrice !== null && suggestedPrice !== undefined) {
            actualPriceInput.value = suggestedPrice;
        }
    }

    function loadProductContext(scope) {
        var productSelector = getProductSelector(scope);
        var contextPanel = scope.querySelector('[data-sales-product-context]');

        if (!productSelector || !contextPanel) {
            return;
        }

        if (!productSelector.value) {
            contextPanel.innerHTML = captureDefaultContextHtml();
            return;
        }

        abp.ajax({
            url: appendProductId(page.dataset.salesContextEndpoint, productSelector.value),
            type: 'GET'
        }).then(function (data) {
            renderContext(scope, data);
        }).catch(function () {
            contextPanel.textContent = l('Sales:ProductContextUnavailable');
        });
    }

    function bindProductSelector(scope, productSelector) {
        function onProductChanged() {
            loadProductContext(scope);
        }

        if (productSelector._vplSalesContextChangeHandler) {
            productSelector.removeEventListener('change', productSelector._vplSalesContextChangeHandler);
        }

        productSelector._vplSalesContextChangeHandler = onProductChanged;
        productSelector.addEventListener('change', onProductChanged);

        if (window.jQuery && window.jQuery.fn.select2) {
            var $select = window.jQuery(productSelector);
            $select.off('select2:select.vplSalesContext select2:clear.vplSalesContext');

            if ($select.data('select2')) {
                $select.on('select2:select.vplSalesContext select2:clear.vplSalesContext', onProductChanged);
            }
        }
    }

    function bindRow(scope) {
        if (!scope || scope.dataset.salesContextBound === 'true') {
            return;
        }

        var productSelector = getProductSelector(scope);

        if (!productSelector) {
            return;
        }

        scope.dataset.salesContextBound = 'true';
        bindProductSelector(scope, productSelector);
        loadProductContext(scope);
    }

    function initializeRow(row) {
        bindRow(row);
    }

    function initializeRows(root) {
        var searchRoot = root || page;

        searchRoot.querySelectorAll('[data-sales-line-row]').forEach(function (row) {
            bindRow(row);
        });

        if (!root) {
            page.querySelectorAll('[data-sales-product-select], [data-sales-product-selector]').forEach(function (selector) {
                var scope = selector.closest('[data-sales-line-row]') || selector.closest('form');

                if (scope && !scope.dataset.salesContextBound) {
                    bindRow(scope);
                }
            });
        }
    }

    window.vplSalesProductContext = {
        initializeRow: initializeRow,
        initializeRows: initializeRows,
        getDefaultContextHtml: captureDefaultContextHtml
    };

    document.addEventListener('DOMContentLoaded', function () {
        captureDefaultContextHtml();

        if (!document.getElementById('SalesCreatePage')) {
            initializeRows();
        }
    });
}(window));
