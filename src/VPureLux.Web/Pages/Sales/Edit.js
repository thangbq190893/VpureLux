(function () {
    const page = document.getElementById('SalesEditPage');
    if (!page) {
        return;
    }

    const productSelector = page.querySelector('[data-sales-product-selector]');
    const actualPriceInput = page.querySelector('[data-sales-actual-price]');
    const contextPanel = page.querySelector('[data-sales-product-context]');
    const l = abp.localization.getResource('VPureLux');

    function appendProductId(url, productId) {
        return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'productId=' + encodeURIComponent(productId);
    }

    function getValue(data, key) {
        const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        return data[key] ?? data[camelKey];
    }

    function renderContext(data) {
        const productLabel = getValue(data, 'ProductLabel') || l('Sales:ProductContextUnavailable');
        const bomStatusText = getValue(data, 'BomStatusText') || l('Sales:ProductContextUnavailable');
        const suggestedPrice = getValue(data, 'SuggestedPrice');
        const suggestedPriceText = suggestedPrice === null || suggestedPrice === undefined
            ? l('Sales:NoSuggestedPrice')
            : suggestedPrice;

        contextPanel.textContent = productLabel + ' | ' + bomStatusText + ' | ' + l('Sales:SuggestedPrice') + ': ' + suggestedPriceText;

        if (actualPriceInput && !actualPriceInput.value && suggestedPrice !== null && suggestedPrice !== undefined) {
            actualPriceInput.value = suggestedPrice;
        }
    }

    function loadProductContext() {
        if (!productSelector?.value || !contextPanel) {
            return;
        }

        abp.ajax({
            url: appendProductId(page.dataset.salesContextEndpoint, productSelector.value),
            type: 'GET'
        }).then(renderContext);
    }

    productSelector?.addEventListener('change', loadProductContext);
    loadProductContext();
})();
