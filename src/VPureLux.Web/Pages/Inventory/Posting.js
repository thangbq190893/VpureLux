(function () {
    var l = abp.localization.getResource('VPureLux');
    var dynamicRows = window.vplDynamicRowSelects;
    var templateAttribute = dynamicRows ? dynamicRows.templateAttribute : 'data-dynamic-row-template';
    var rowSelector = '[data-inventory-line-row]';

    function getLiveRows(container) {
        return container.querySelectorAll(rowSelector + ':not([' + templateAttribute + '])');
    }

    function usesHtmlRowTemplate(container) {
        var templateId = container.dataset.rowTemplate;

        return !!templateId && !!document.getElementById(templateId);
    }

    function removeLegacyTemplateRows(container) {
        container.querySelectorAll('[' + templateAttribute + ']').forEach(function (row) {
            row.remove();
        });
    }

    function prepareLineSelects(container, row) {
        dynamicRows.stripSelect2Enhancements(row);

        if (!usesHtmlRowTemplate(container)) {
            dynamicRows.initializeSelects(row);
        }
    }

    function cloneInventoryRow(container) {
        if (usesHtmlRowTemplate(container)) {
            var templateElement = document.getElementById(container.dataset.rowTemplate);

            return dynamicRows.createCleanClone(templateElement.content.firstElementChild.cloneNode(true));
        }

        var template = dynamicRows.ensureTemplate(container, rowSelector);

        if (!template) {
            return null;
        }

        var row = dynamicRows.createCleanClone(template);
        row.classList.remove('d-none');
        row.removeAttribute(templateAttribute);
        row.removeAttribute('aria-hidden');
        return row;
    }

    function applyTemplate(element, attributeName, index) {
        var template = element.getAttribute(attributeName);

        if (template) {
            element.setAttribute(attributeName.replace('data-', ''), template.replace(/__index__/g, index));
        }
    }

    function reindexRows(container) {
        getLiveRows(container).forEach(function (row, index) {
            row.querySelectorAll('[data-name]').forEach(function (element) {
                applyTemplate(element, 'data-name', index);
            });

            row.querySelectorAll('[data-id]').forEach(function (element) {
                applyTemplate(element, 'data-id', index);
            });

            row.querySelectorAll('[data-for]').forEach(function (element) {
                applyTemplate(element, 'data-for', index);
            });

            row.querySelectorAll('[data-valmsg-for-template]').forEach(function (element) {
                element.setAttribute('data-valmsg-for', element.getAttribute('data-valmsg-for-template').replace(/__index__/g, index));
            });
        });
    }

    function clearRow(row) {
        row.querySelectorAll('select').forEach(function (select) {
            select.selectedIndex = 0;
        });

        row.querySelectorAll('input').forEach(function (input) {
            if (input.type === 'hidden') {
                return;
            }

            if (input.dataset.defaultValue !== undefined) {
                input.value = input.dataset.defaultValue;
                return;
            }

            input.value = '';
        });
    }

    function initializeLineCollection(container) {
        var addButtonSelector = container.dataset.addButton;
        var addButton = addButtonSelector ? document.querySelector(addButtonSelector) : null;

        if (dynamicRows) {
            if (usesHtmlRowTemplate(container)) {
                removeLegacyTemplateRows(container);
            } else {
                dynamicRows.ensureTemplate(container, rowSelector);
            }

            getLiveRows(container).forEach(function (row) {
                prepareLineSelects(container, row);
            });
        }

        if (addButton) {
            addButton.addEventListener('click', function () {
                var row;

                if (dynamicRows) {
                    row = cloneInventoryRow(container);

                    if (!row) {
                        return;
                    }
                } else {
                    var source = container.querySelector(rowSelector);

                    if (!source) {
                        return;
                    }

                    row = source.cloneNode(true);
                }

                clearRow(row);
                container.appendChild(row);
                reindexRows(container);

                if (dynamicRows) {
                    prepareLineSelects(container, row);
                }
            });
        }

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('[data-remove-line]');

            if (!removeButton) {
                return;
            }

            if (getLiveRows(container).length <= 1) {
                return;
            }

            var row = removeButton.closest(rowSelector);

            if (row && dynamicRows) {
                dynamicRows.stripSelect2Enhancements(row);
            }

            row.remove();
            reindexRows(container);
        });

        reindexRows(container);
    }

    function initializeAdjustmentType(page) {
        var typeSelector = page.querySelector('[data-adjustment-type]');
        var increaseSection = page.querySelector('[data-adjustment-increase-section]');
        var decreaseSection = page.querySelector('[data-adjustment-decrease-section]');

        if (!typeSelector || !increaseSection || !decreaseSection) {
            return;
        }

        function sync() {
            var isIncrease = typeSelector.value === page.dataset.adjustmentIncreaseValue;
            increaseSection.classList.toggle('d-none', !isIncrease);
            decreaseSection.classList.toggle('d-none', isIncrease);

            increaseSection.querySelectorAll('input, select').forEach(function (element) {
                element.disabled = !isIncrease;
            });

            decreaseSection.querySelectorAll('input, select').forEach(function (element) {
                element.disabled = isIncrease;
            });
        }

        typeSelector.addEventListener('change', sync);
        sync();
    }

    function initializeCountAdjustment(page) {
        var warehouseSelector = document.querySelector('[data-adjustment-warehouse]');
        var balanceDataElement = document.getElementById('adjustment-balance-data');
        var balances = [];

        if (!warehouseSelector || !balanceDataElement) {
            return;
        }

        try {
            balances = JSON.parse(balanceDataElement.textContent || '[]');
        } catch (error) {
            balances = [];
        }

        function parseDecimal(value) {
            if (value === undefined || value === null || value === '') {
                return null;
            }

            var parsed = Number(String(value).replace(',', '.'));
            return Number.isFinite(parsed) ? parsed : null;
        }

        function formatDecimal(value) {
            if (value === null || value === undefined || !Number.isFinite(value)) {
                return '';
            }

            if (Math.abs(value) < 0.00005) {
                value = 0;
            }

            return String(Math.round(value * 10000) / 10000);
        }

        function getCurrentQuantity(stockItemId) {
            var warehouseId = warehouseSelector.value;

            if (!warehouseId || !stockItemId) {
                return null;
            }

            var match = balances.find(function (balance) {
                return balance.WarehouseId === warehouseId && balance.StockItemId === stockItemId;
            });

            return match ? parseDecimal(match.Quantity) || 0 : 0;
        }

        function setPositiveFields(row, isPositive) {
            row.querySelectorAll('[data-positive-delta-field]').forEach(function (field) {
                field.classList.toggle('d-none', !isPositive);
                field.querySelectorAll('input, select').forEach(function (element) {
                    element.disabled = !isPositive;
                });
            });
        }

        function setDirection(row, delta) {
            var direction = row.querySelector('[data-direction-label]');

            if (!direction) {
                return;
            }

            direction.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-secondary');

            if (delta > 0) {
                direction.textContent = page.dataset.increaseLabel;
                direction.classList.add('text-bg-success');
                return;
            }

            if (delta < 0) {
                direction.textContent = page.dataset.decreaseLabel;
                direction.classList.add('text-bg-danger');
                return;
            }

            direction.textContent = page.dataset.noChangeLabel;
            direction.classList.add('text-bg-secondary');
        }

        function updateRow(row) {
            var stockItem = row.querySelector('[data-count-stock-item]');
            var currentInput = row.querySelector('[data-current-quantity]');
            var countedInput = row.querySelector('[data-counted-quantity]');
            var deltaInput = row.querySelector('[data-delta]');
            var currentQuantity = stockItem ? getCurrentQuantity(stockItem.value) : null;
            var countedQuantity = countedInput ? parseDecimal(countedInput.value) : null;
            var delta = currentQuantity !== null && countedQuantity !== null
                ? countedQuantity - currentQuantity
                : 0;

            if (currentInput) {
                currentInput.value = currentQuantity === null ? '' : formatDecimal(currentQuantity);
            }

            if (deltaInput) {
                deltaInput.value = countedQuantity === null || currentQuantity === null ? '' : formatDecimal(delta);
            }

            setDirection(row, countedQuantity === null || currentQuantity === null ? 0 : delta);
            setPositiveFields(row, countedQuantity !== null && currentQuantity !== null && delta > 0);
        }

        function updateAllRows() {
            document.querySelectorAll('[data-count-adjustment-row]').forEach(updateRow);
        }

        document.addEventListener('input', function (event) {
            var row = event.target.closest('[data-count-adjustment-row]');

            if (row) {
                updateRow(row);
            }
        });

        document.addEventListener('change', function (event) {
            var row = event.target.closest('[data-count-adjustment-row]');

            if (event.target === warehouseSelector) {
                updateAllRows();
                return;
            }

            if (row) {
                updateRow(row);
            }
        });

        document.addEventListener('click', function (event) {
            if (event.target.closest('[data-remove-line], .vpl-line-editor-action')) {
                window.setTimeout(updateAllRows, 0);
            }
        });

        updateAllRows();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-inventory-line-container]').forEach(initializeLineCollection);

        document.querySelectorAll('[data-inventory-posting-page]').forEach(function (page) {
            if (page.dataset.postSuccess) {
                abp.notify.success(page.dataset.postSuccess);
            }

            initializeAdjustmentType(page);
            initializeCountAdjustment(page);
        });

        document.querySelectorAll('[data-inventory-posting-form]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (form.dataset.confirmed === 'true') {
                    return;
                }

                event.preventDefault();

                abp.message.confirm(form.dataset.confirmMessage, l('Confirm')).then(function (confirmed) {
                    if (!confirmed) {
                        return;
                    }

                    form.dataset.confirmed = 'true';
                    abp.ui.setBusy(form);
                    form.submit();
                });
            });
        });
    });

    if (window.abp && abp.dom && typeof abp.dom.ready === 'function') {
        abp.dom.ready(function () {
            if (!dynamicRows) {
                return;
            }

            document.querySelectorAll('[data-inventory-line-container]').forEach(function (container) {
                if (!usesHtmlRowTemplate(container)) {
                    return;
                }

                removeLegacyTemplateRows(container);

                getLiveRows(container).forEach(function (row) {
                    prepareLineSelects(container, row);
                });
            });
        });
    }
}());
