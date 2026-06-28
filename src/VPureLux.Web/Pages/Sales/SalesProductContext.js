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

    function getSelectedProductId(productSelector) {
        return productSelector ? productSelector.value || '' : '';
    }

    function setPreviousProductId(productSelector, productId) {
        if (productSelector) {
            productSelector.dataset.salesPreviousProductId = productId || '';
        }
    }

    function hasSelectedProductChanged(productSelector) {
        if (!productSelector) {
            return false;
        }

        return (productSelector.dataset.salesPreviousProductId || '') !== getSelectedProductId(productSelector);
    }

    function getNotEligibleMessage() {
        if (createPage && createPage.dataset.salesProductNotEligible) {
            return createPage.dataset.salesProductNotEligible;
        }

        return l('Sales:ProductNotSaleEligible');
    }

    function getOverrideReasonRequiredMessage() {
        if (createPage && createPage.dataset.salesOverrideReasonRequired) {
            return createPage.dataset.salesOverrideReasonRequired;
        }

        return l('SALES_009');
    }

    function getManualPriceRequiredMessage() {
        return l('Sales:ManualPriceRequired');
    }

    function getCreateAlert() {
        return createPage ? createPage.querySelector('[data-sales-create-alert]') : null;
    }

    function showCreateAlert(message) {
        var alert = getCreateAlert();

        if (!alert) {
            return;
        }

        alert.textContent = message;
        alert.classList.remove('d-none');
    }

    function clearCreateAlert() {
        var alert = getCreateAlert();

        if (!alert) {
            return;
        }

        alert.textContent = '';
        alert.classList.add('d-none');
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

    function clearOverrideValidation(scope) {
        var message = scope.querySelector('[data-sales-override-validation]');
        var overrideInput = scope.querySelector('.sales-line-override');

        if (message) {
            message.textContent = '';
            message.classList.add('d-none');
        }

        if (overrideInput) {
            overrideInput.classList.remove('is-invalid');
        }
    }

    function markActualPriceAutoFilled(actualPriceInput, isAutoFilled) {
        if (actualPriceInput) {
            actualPriceInput.dataset.salesPriceAutoFilled = isAutoFilled ? 'true' : 'false';
        }
    }

    function clearOverrideReason(scope) {
        var overrideInput = scope.querySelector('.sales-line-override');

        if (!overrideInput) {
            return;
        }

        overrideInput.value = '';
        overrideInput.classList.remove('is-invalid');
    }

    function parseMoney(value) {
        if (value === null || value === undefined || value === '') {
            return null;
        }

        var parsed = Number(String(value).replace(',', '.'));
        if (!Number.isFinite(parsed)) {
            return null;
        }

        return Math.round(parsed * 100) / 100;
    }

    function validateOverrideReason(scope, data) {
        clearOverrideValidation(scope);

        if (!data) {
            return true;
        }

        var suggestedPrice = parseMoney(getValue(data, 'SuggestedPrice'));
        if (suggestedPrice === null) {
            return true;
        }

        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');
        var actualPrice = actualPriceInput ? parseMoney(actualPriceInput.value) : null;
        if (actualPrice === null || actualPrice === suggestedPrice) {
            return true;
        }

        var overrideInput = scope.querySelector('.sales-line-override');
        if (overrideInput && overrideInput.value.trim()) {
            return true;
        }

        var messageText = getOverrideReasonRequiredMessage();
        var message = scope.querySelector('[data-sales-override-validation]');

        if (message) {
            message.textContent = messageText;
            message.classList.remove('d-none');
        }

        if (overrideInput) {
            overrideInput.classList.add('is-invalid');
        }

        return false;
    }

    function resetLinePricingForProductChange(scope, data) {
        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');
        var suggestedPrice = data ? getValue(data, 'SuggestedPrice') : null;

        if (actualPriceInput) {
            if (suggestedPrice === null || suggestedPrice === undefined) {
                actualPriceInput.value = '';
                markActualPriceAutoFilled(actualPriceInput, false);
            } else {
                actualPriceInput.value = suggestedPrice;
                markActualPriceAutoFilled(actualPriceInput, true);
            }
        }

        clearOverrideReason(scope);
        clearOverrideValidation(scope);
    }

    function getSuggestedPriceText(suggestedPrice) {
        if (suggestedPrice === null || suggestedPrice === undefined) {
            return l('Sales:NoSuggestedPrice') + ' - ' + getManualPriceRequiredMessage();
        }

        return suggestedPrice;
    }

    function renderContext(scope, data, options) {
        options = options || {};
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
        var suggestedPriceText = getSuggestedPriceText(suggestedPrice);

        contextPanel.innerHTML =
            '<div class="mb-2"><span class="' + bomBadgeClass + '">' + escapeHtml(bomText) + '</span></div>' +
            '<div class="small text-muted mb-1">' + escapeHtml(l('Sales:ProductImage')) + ': ' + escapeHtml(imageText) + '</div>' +
            '<div class="small">' + escapeHtml(l('Sales:SuggestedPrice')) + ': ' + escapeHtml(String(suggestedPriceText)) + '</div>';

        if (options.resetPricing) {
            resetLinePricingForProductChange(scope, data);
        } else if (actualPriceInput && !actualPriceInput.value && suggestedPrice !== null && suggestedPrice !== undefined) {
            actualPriceInput.value = suggestedPrice;
            markActualPriceAutoFilled(actualPriceInput, true);
        }

        clearOverrideValidation(scope);
        updateRowEligibility(scope, data);
    }

    function renderPlaceholder(scope, options) {
        options = options || {};
        var contextPanel = scope.querySelector('[data-sales-product-context]');

        if (contextPanel) {
            contextPanel.innerHTML = captureDefaultContextHtml();
        }

        if (options.resetPricing) {
            resetLinePricingForProductChange(scope, null);
        }

        clearOverrideValidation(scope);
        updateRowEligibility(scope, null);
    }

    function loadProductContextFromMap(scope, productId, options) {
        var map = getProductContextMap();
        var data = map[productId];

        if (data) {
            renderContext(scope, data, options);
            return true;
        }

        renderContext(scope, { HasPublishedBom: false, HasImage: false, SuggestedPrice: null }, options);
        return true;
    }

    function loadProductContext(scope, options) {
        options = options || {};
        var productSelector = getProductSelector(scope);
        var contextPanel = scope.querySelector('[data-sales-product-context]');

        if (!productSelector || !contextPanel) {
            return;
        }

        if (!productSelector.value) {
            renderPlaceholder(scope, options);
            return;
        }

        if (createPage && Object.keys(getProductContextMap()).length > 0) {
            loadProductContextFromMap(scope, productSelector.value, options);
            return;
        }

        abp.ajax({
            url: appendProductId(page.dataset.salesContextEndpoint, productSelector.value),
            type: 'GET'
        }).then(function (data) {
            renderContext(scope, data, options);
        }).catch(function () {
            contextPanel.textContent = l('Sales:ProductContextUnavailable');
            updateRowEligibility(scope, null);
        });
    }

    function bindProductSelector(scope, productSelector) {
        function onProductChanged() {
            var productChanged = hasSelectedProductChanged(productSelector);
            var selectedProductId = getSelectedProductId(productSelector);

            setPreviousProductId(productSelector, selectedProductId);

            if (createPage && productChanged) {
                clearCreateAlert();
            }

            loadProductContext(scope, { resetPricing: !!createPage && productChanged });
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

    function bindActualPriceInput(scope) {
        var actualPriceInput = scope.querySelector('[data-sales-actual-price]');

        if (!actualPriceInput) {
            return;
        }

        if (actualPriceInput._vplSalesActualPriceInputHandler) {
            actualPriceInput.removeEventListener('input', actualPriceInput._vplSalesActualPriceInputHandler);
        }

        actualPriceInput._vplSalesActualPriceInputHandler = function () {
            markActualPriceAutoFilled(actualPriceInput, false);
        };
        actualPriceInput.addEventListener('input', actualPriceInput._vplSalesActualPriceInputHandler);
    }

    function bindRow(scope) {
        if (!scope) {
            return;
        }

        var productSelector = getProductSelector(scope);

        if (!productSelector) {
            return;
        }

        scope.dataset.salesContextBound = 'true';
        bindProductSelector(scope, productSelector);
        bindActualPriceInput(scope);
        setPreviousProductId(productSelector, getSelectedProductId(productSelector));
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
        var alertMessage = '';
        clearCreateAlert();

        container.querySelectorAll('[data-sales-line-row]').forEach(function (row) {
            var productSelector = getProductSelector(row);

            if (!productSelector || !productSelector.value) {
                clearOverrideValidation(row);
                return;
            }

            loadProductContext(row);

            if (createPage && Object.keys(getProductContextMap()).length > 0) {
                var data = getProductContextMap()[productSelector.value];

                if (!data || !hasPublishedBom(data)) {
                    isValid = false;
                    alertMessage = alertMessage || getNotEligibleMessage();
                }

                if (!validateOverrideReason(row, data)) {
                    isValid = false;
                    alertMessage = alertMessage || getOverrideReasonRequiredMessage();
                }
            }
        });

        if (!isValid && alertMessage) {
            showCreateAlert(alertMessage);
        }

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
