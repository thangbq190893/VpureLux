(function () {
    var dynamicRows = window.vplDynamicRowSelects;
    var productContext = window.vplSalesProductContext;
    var templateAttribute = dynamicRows ? dynamicRows.templateAttribute : 'data-dynamic-row-template';
    var rowSelector = '[data-sales-line-row]';

    function getLiveRows(container) {
        return container.querySelectorAll(rowSelector + ':not([' + templateAttribute + '])');
    }

    function reindexRows(container) {
        getLiveRows(container).forEach(function (row, index) {
            var product = row.querySelector('.sales-line-product');
            var quantity = row.querySelector('.sales-line-quantity');
            var actualPrice = row.querySelector('.sales-line-actual-price');
            var overrideReason = row.querySelector('.sales-line-override');

            if (product) {
                product.name = 'Input.Lines[' + index + '].ProductId';
                product.id = 'Input_Lines_' + index + '__ProductId';
            }

            if (quantity) {
                quantity.name = 'Input.Lines[' + index + '].Quantity';
                quantity.id = 'Input_Lines_' + index + '__Quantity';
            }

            if (actualPrice) {
                actualPrice.name = 'Input.Lines[' + index + '].ActualSellingPrice';
                actualPrice.id = 'Input_Lines_' + index + '__ActualSellingPrice';
            }

            if (overrideReason) {
                overrideReason.name = 'Input.Lines[' + index + '].OverrideReason';
                overrideReason.id = 'Input_Lines_' + index + '__OverrideReason';
            }

            row.querySelectorAll('label[for^="Input_Lines_"]').forEach(function (label) {
                var fieldSuffix = label.getAttribute('for').split('__').pop();
                if (fieldSuffix) {
                    label.setAttribute('for', 'Input_Lines_' + index + '__' + fieldSuffix);
                }
            });
        });
    }

    function clearRow(row) {
        var product = row.querySelector('.sales-line-product');
        var quantity = row.querySelector('.sales-line-quantity');
        var actualPrice = row.querySelector('.sales-line-actual-price');
        var overrideReason = row.querySelector('.sales-line-override');
        var contextPanel = row.querySelector('[data-sales-product-context]');

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

        if (contextPanel && productContext && typeof productContext.getDefaultContextHtml === 'function') {
            contextPanel.innerHTML = productContext.getDefaultContextHtml();
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var container = document.getElementById('sales-create-lines');
        var addButton = document.getElementById('add-sales-line');

        if (!container || !addButton || !dynamicRows) {
            return;
        }

        dynamicRows.ensureTemplate(container, rowSelector);

        addButton.addEventListener('click', function () {
            var template = dynamicRows.ensureTemplate(container, rowSelector);

            if (!template) {
                return;
            }

            var liveRow = container.querySelector(rowSelector + ':not([' + templateAttribute + '])');
            var sourceProduct = liveRow ? liveRow.querySelector('.sales-line-product') : null;
            var row = dynamicRows.createCleanClone(template);

            row.classList.remove('d-none');
            row.removeAttribute(templateAttribute);
            row.removeAttribute('aria-hidden');
            row.removeAttribute('data-sales-context-bound');

            var product = row.querySelector('.sales-line-product');

            if (product && sourceProduct) {
                product.innerHTML = sourceProduct.innerHTML;
            }

            clearRow(row);
            container.appendChild(row);
            reindexRows(container);
            dynamicRows.initializeSelects(row);

            if (productContext && typeof productContext.initializeRow === 'function') {
                productContext.initializeRow(row);
            }
        });

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('.remove-sales-line');

            if (removeButton && getLiveRows(container).length > 1) {
                removeButton.closest(rowSelector).remove();
                reindexRows(container);
            }
        });
    });
}());
