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

        var panel = page.querySelector('[data-sales-product-context]');
        defaultContextHtml = panel ? panel.innerHTML : l('Sales:SelectProductForContext');
        return defaultContextHtml;
    }

    function renderContext(scope, data) {
        var contextPanel = scope.querySelector('[data-sales-product-context]');
        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');

        if (!contextPanel) {
            return;
        }

        var productLabel = getValue(data, 'ProductLabel') || l('Sales:ProductContextUnavailable');
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
            '<div class="fw-semibold mb-1">' + escapeHtml(productLabel) + '</div>' +
            '<div class="mb-2"><span class="' + bomBadgeClass + '">' + escapeHtml(bomText) + '</span></div>' +
            '<div class="small text-muted mb-1">' + escapeHtml(l('Sales:ProductImage')) + ': ' + escapeHtml(imageText) + '</div>' +
            '<div class="small">' + escapeHtml(l('Sales:SuggestedPrice')) + ': ' + escapeHtml(String(suggestedPriceText)) + '</div>';

        if (actualPriceInput && !actualPriceInput.value && suggestedPrice !== null && suggestedPrice !== undefined) {
            actualPriceInput.value = suggestedPrice;
        }
    }

    function loadProductContext(scope) {
        var productSelector = scope.querySelector('[data-sales-product-selector]');
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

    function getRowScope(selector) {
        return selector.closest('[data-sales-line-row]') || selector.closest('form') || page;
    }

    function bindRow(scope) {
        if (!scope || scope.dataset.salesContextBound === 'true') {
            return;
        }

        var productSelector = scope.querySelector('[data-sales-product-selector]');

        if (!productSelector) {
            return;
        }

        scope.dataset.salesContextBound = 'true';
        productSelector.addEventListener('change', function () {
            loadProductContext(scope);
        });
        loadProductContext(scope);
    }

    function initializeRow(row) {
        bindRow(row);
    }

    function initializeRows(root) {
        var searchRoot = root || page;

        searchRoot.querySelectorAll('[data-sales-line-row]:not([data-dynamic-row-template])').forEach(function (row) {
            bindRow(row);
        });

        if (!root) {
            page.querySelectorAll('[data-sales-product-selector]').forEach(function (selector) {
                var scope = getRowScope(selector);

                if (!scope.hasAttribute('data-sales-line-row')) {
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
        initializeRows();
    });
}(window));
