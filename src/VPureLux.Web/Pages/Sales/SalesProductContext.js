(function (window) {
    var page = document.getElementById('SalesCreatePage') || document.getElementById('SalesEditPage');
    if (!page) {
        return;
    }

    var l = abp.localization.getResource('VPureLux');
    var defaultContextHtml = null;
    var productContextMap = null;
    var createPage = document.getElementById('SalesCreatePage');

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

    function getProductContextMap() {
        if (productContextMap !== null) {
            return productContextMap;
        }

        productContextMap = {};
        var dataElement = document.getElementById('sales-product-context-data');

        if (dataElement && dataElement.textContent) {
            try {
                productContextMap = JSON.parse(dataElement.textContent);
            } catch (error) {
                productContextMap = {};
            }
        }

        return productContextMap;
    }

    function getProductSelector(scope) {
        return scope.querySelector('[data-sales-product-select]')
            || scope.querySelector('[data-sales-product-selector]');
    }

    function getNotEligibleMessage() {
        if (createPage && createPage.dataset.salesProductNotEligible) {
            return createPage.dataset.salesProductNotEligible;
        }

        return l('Sales:ProductNotSaleEligible');
    }

    function hasPublishedBom(data) {
        return getValue(data, 'HasPublishedBom') === true || getValue(data, 'HasPublishedBom') === 'true';
    }

    function updateRowEligibility(scope, data) {
        var warning = scope.querySelector('[data-sales-product-eligibility]');
        var productSelector = getProductSelector(scope);
        var isEligible = data ? hasPublishedBom(data) : true;
        var showWarning = data && !isEligible;

        scope.classList.toggle('sales-line-invalid', !!showWarning);

        if (productSelector) {
            productSelector.classList.toggle('is-invalid', !!showWarning);
        }

        if (!warning) {
            return;
        }

        if (showWarning) {
            warning.textContent = getNotEligibleMessage();
            warning.classList.remove('d-none');
            return;
        }

        warning.textContent = '';
        warning.classList.add('d-none');
    }

    function renderContext(scope, data) {
        var contextPanel = scope.querySelector('[data-sales-product-context]');
        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');

        if (!contextPanel) {
            return;
        }

        var published = hasPublishedBom(data);
        var hasImage = getValue(data, 'HasImage') === true || getValue(data, 'HasImage') === 'true';
        var suggestedPrice = getValue(data, 'SuggestedPrice');
        var bomBadgeClass = published ? 'badge bg-success' : 'badge bg-warning text-dark';
        var bomText = published ? l('Sales:PublishedBomAvailable') : l('Sales:NoPublishedBom');
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

        updateRowEligibility(scope, data);
    }

    function renderPlaceholder(scope) {
        var contextPanel = scope.querySelector('[data-sales-product-context]');

        if (contextPanel) {
            contextPanel.innerHTML = captureDefaultContextHtml();
        }

        updateRowEligibility(scope, null);
    }

    function loadProductContextFromMap(scope, productId) {
        var map = getProductContextMap();
        var data = map[productId];

        if (data) {
            renderContext(scope, data);
            return true;
        }

        renderContext(scope, { HasPublishedBom: false, HasImage: false, SuggestedPrice: null });
        return true;
    }

    function loadProductContext(scope) {
        var productSelector = getProductSelector(scope);
        var contextPanel = scope.querySelector('[data-sales-product-context]');

        if (!productSelector || !contextPanel) {
            return;
        }

        if (!productSelector.value) {
            renderPlaceholder(scope);
            return;
        }

        if (createPage && Object.keys(getProductContextMap()).length > 0) {
            loadProductContextFromMap(scope, productSelector.value);
            return;
        }

        abp.ajax({
            url: appendProductId(page.dataset.salesContextEndpoint, productSelector.value),
            type: 'GET'
        }).then(function (data) {
            renderContext(scope, data);
        }).catch(function () {
            contextPanel.textContent = l('Sales:ProductContextUnavailable');
            updateRowEligibility(scope, null);
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

    function validateAllRows(container) {
        if (!container) {
            return true;
        }

        var isValid = true;

        container.querySelectorAll('[data-sales-line-row]').forEach(function (row) {
            var productSelector = getProductSelector(row);

            if (!productSelector || !productSelector.value) {
                return;
            }

            loadProductContext(row);

            if (createPage && Object.keys(getProductContextMap()).length > 0) {
                var data = getProductContextMap()[productSelector.value];

                if (!data || !hasPublishedBom(data)) {
                    isValid = false;
                }
            }
        });

        return isValid;
    }

    window.vplSalesProductContext = {
        initializeRow: initializeRow,
        initializeRows: initializeRows,
        getDefaultContextHtml: captureDefaultContextHtml,
        validateAllRows: validateAllRows,
        loadProductContext: loadProductContext
    };

    document.addEventListener('DOMContentLoaded', function () {
        captureDefaultContextHtml();
        getProductContextMap();

        if (!createPage) {
            initializeRows();
        }
    });
}(window));
