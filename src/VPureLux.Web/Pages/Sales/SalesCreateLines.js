(function () {
    var dynamicRows = window.vplDynamicRowSelects;
    var productContext = window.vplSalesProductContext;
    var rowSelector = '[data-sales-line-row]';
    var indexToken = '__index__';

    function getLiveRows(container) {
        return container.querySelectorAll(rowSelector);
    }

    function applyTemplateAttribute(element, attributeName, index) {
        var template = element.getAttribute(attributeName);

        if (template) {
            element.setAttribute(attributeName.replace('data-', ''), template.replace(new RegExp(indexToken, 'g'), index));
        }
    }

    function reindexRows(container) {
        getLiveRows(container).forEach(function (row, index) {
            row.querySelectorAll('[data-name]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-name', index);
            });

            row.querySelectorAll('[data-id]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-id', index);
            });

            row.querySelectorAll('[data-for]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-for', index);
            });
        });
    }

    function resetContextPanel(row) {
        var contextPanel = row.querySelector('[data-sales-product-context]');

        if (!contextPanel) {
            return;
        }

        if (productContext && typeof productContext.getDefaultContextHtml === 'function') {
            contextPanel.innerHTML = productContext.getDefaultContextHtml();
            return;
        }

        contextPanel.textContent = '';
    }

    function clearRow(row) {
        var product = row.querySelector('[data-sales-product-select]');
        var quantity = row.querySelector('.sales-line-quantity');
        var actualPrice = row.querySelector('.sales-line-actual-price');
        var overrideReason = row.querySelector('.sales-line-override');

        if (product) {
            product.selectedIndex = 0;
        }

        if (quantity) {
            quantity.value = '';
        }

        if (actualPrice) {
            actualPrice.value = '';
        }

        if (overrideReason) {
            overrideReason.value = '';
        }

        resetContextPanel(row);
    }

    function cloneTemplateRow() {
        var template = document.getElementById('sales-line-row-template');

        if (!template || !template.content || !template.content.firstElementChild) {
            return null;
        }

        return template.content.firstElementChild.cloneNode(true);
    }

    function initializeProductSelect(row) {
        var product = row.querySelector('[data-sales-product-select]');

        if (!product || !dynamicRows) {
            return;
        }

        dynamicRows.initializeSelects(product);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var container = document.getElementById('sales-create-lines');
        var addButton = document.getElementById('add-sales-line');

        if (!container || !addButton) {
            return;
        }

        addButton.addEventListener('click', function () {
            var row = cloneTemplateRow();

            if (!row) {
                return;
            }

            row.removeAttribute('data-sales-context-bound');
            clearRow(row);
            container.appendChild(row);
            reindexRows(container);
            initializeProductSelect(row);

            if (productContext && typeof productContext.initializeRow === 'function') {
                productContext.initializeRow(row);
            }
        });

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('.remove-sales-line');

            if (removeButton && getLiveRows(container).length > 1) {
                var row = removeButton.closest(rowSelector);
                var product = row ? row.querySelector('[data-sales-product-select]') : null;

                if (product && dynamicRows) {
                    dynamicRows.stripSelect2Enhancements(row);
                }

                row.remove();
                reindexRows(container);
            }
        });
    });
}());
