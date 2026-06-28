(function () {
    var dynamicRows = window.vplDynamicRowSelects;
    var productContext = window.vplSalesProductContext;
    var rowSelector = '[data-sales-line-row]';
    var productSelectSelector = '[data-sales-product-select]';
    var indexToken = '__index__';
    var defaultQuantity = '1';

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
            row.setAttribute('data-sales-line-index', String(index));

            row.querySelectorAll('[data-name]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-name', index);
            });

            row.querySelectorAll('[data-id]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-id', index);
            });

            row.querySelectorAll('[data-for]').forEach(function (element) {
                applyTemplateAttribute(element, 'data-for', index);
            });

            var productSelect = row.querySelector(productSelectSelector);

            if (productSelect) {
                productSelect.setAttribute('data-sales-line-index', String(index));
            }
        });
    }

    function resetContextPanel(row) {
        var contextPanel = row.querySelector('[data-sales-product-context]');
        var eligibilityWarning = row.querySelector('[data-sales-product-eligibility]');
        var overrideValidation = row.querySelector('[data-sales-override-validation]');
        var overrideInput = row.querySelector('.sales-line-override');
        var actualPrice = row.querySelector('.sales-line-actual-price');

        if (eligibilityWarning) {
            eligibilityWarning.textContent = '';
            eligibilityWarning.classList.add('d-none');
        }

        if (overrideValidation) {
            overrideValidation.textContent = '';
            overrideValidation.classList.add('d-none');
        }

        if (overrideInput) {
            overrideInput.classList.remove('is-invalid');
        }

        if (actualPrice) {
            delete actualPrice.dataset.salesPriceAutoFilled;
        }

        row.classList.remove('sales-line-invalid');

        var product = row.querySelector(productSelectSelector);

        if (product) {
            product.classList.remove('is-invalid');
        }

        if (!contextPanel) {
            return;
        }

        if (productContext && typeof productContext.getDefaultContextHtml === 'function') {
            contextPanel.innerHTML = productContext.getDefaultContextHtml();
            return;
        }

        contextPanel.textContent = '';
    }

    function ensureNativeProductSelect(row) {
        var product = row.querySelector(productSelectSelector);

        if (!product) {
            return;
        }

        if (dynamicRows) {
            dynamicRows.stripSelect2Enhancements(row);
        }

        product.classList.add('form-select', 'w-100');
    }

    function clearRow(row) {
        var product = row.querySelector(productSelectSelector);
        var quantity = row.querySelector('.sales-line-quantity');
        var actualPrice = row.querySelector('.sales-line-actual-price');
        var overrideReason = row.querySelector('.sales-line-override');

        if (product) {
            product.selectedIndex = 0;
            delete product.dataset.salesPreviousProductId;
        }

        if (quantity) {
            quantity.value = defaultQuantity;
        }

        if (actualPrice) {
            actualPrice.value = '';
            delete actualPrice.dataset.salesPriceAutoFilled;
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

    function prepareLineRow(row) {
        row.removeAttribute('data-sales-context-bound');
        ensureNativeProductSelect(row);

        if (productContext && typeof productContext.initializeRow === 'function') {
            productContext.initializeRow(row);
        }
    }

    function bootExistingRows(container) {
        getLiveRows(container).forEach(prepareLineRow);
    }

    function whenAbpDomReady(callback) {
        if (window.abp && abp.event && typeof abp.event.on === 'function') {
            abp.event.on('abp.dom.ready', callback);
            return;
        }

        callback();
    }

    document.addEventListener('DOMContentLoaded', function () {
        var container = document.getElementById('sales-create-lines');
        var addButton = document.getElementById('add-sales-line');

        if (!container || !addButton) {
            return;
        }

        bootExistingRows(container);
        whenAbpDomReady(function () {
            bootExistingRows(container);
        });

        var form = document.querySelector('#SalesCreatePage form');

        if (form) {
            form.addEventListener('submit', function (event) {
                if (productContext && typeof productContext.validateAllRows === 'function') {
                    if (!productContext.validateAllRows(container)) {
                        event.preventDefault();
                    }
                }
            });
        }

        addButton.addEventListener('click', function () {
            var row = cloneTemplateRow();

            if (!row) {
                return;
            }

            clearRow(row);
            container.appendChild(row);
            reindexRows(container);
            prepareLineRow(row);
        });

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('.remove-sales-line');

            if (removeButton && getLiveRows(container).length > 1) {
                var row = removeButton.closest(rowSelector);

                if (row && dynamicRows) {
                    dynamicRows.stripSelect2Enhancements(row);
                }

                row.remove();
                reindexRows(container);
            }
        });
    });
}());
