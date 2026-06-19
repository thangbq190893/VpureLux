(function () {
    const page = document.getElementById('SalesCreatePage') || document.getElementById('SalesEditPage');
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

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function renderContext(data) {
        const productLabel = getValue(data, 'ProductLabel') || l('Sales:ProductContextUnavailable');
        const hasPublishedBom = getValue(data, 'HasPublishedBom') === true || getValue(data, 'HasPublishedBom') === 'true';
        const hasImage = getValue(data, 'HasImage') === true || getValue(data, 'HasImage') === 'true';
        const suggestedPrice = getValue(data, 'SuggestedPrice');
        const bomBadgeClass = hasPublishedBom ? 'badge bg-success' : 'badge bg-warning text-dark';
        const bomText = hasPublishedBom ? l('Sales:PublishedBomAvailable') : l('Sales:NoPublishedBom');
        const imageText = hasImage ? l('Sales:HasProductImage') : l('Sales:NoProductImage');
        const suggestedPriceText = suggestedPrice === null || suggestedPrice === undefined
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

    function loadProductContext() {
        if (!productSelector?.value || !contextPanel) {
            return;
        }

        abp.ajax({
            url: appendProductId(page.dataset.salesContextEndpoint, productSelector.value),
            type: 'GET'
        }).then(renderContext).catch(function () {
            contextPanel.textContent = l('Sales:ProductContextUnavailable');
        });
    }

    productSelector?.addEventListener('change', loadProductContext);
    loadProductContext();
})();
