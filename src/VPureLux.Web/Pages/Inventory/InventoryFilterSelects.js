(function () {
    function initializeInventoryFilterSelects() {
        if (!window.vplDynamicRowSelects) {
            return;
        }

        window.vplDynamicRowSelects.initializeSelects(document, '.inventory-filter-stock-item');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeInventoryFilterSelects);
        return;
    }

    initializeInventoryFilterSelects();
})();
