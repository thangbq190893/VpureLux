(function () {
    var l = abp.localization.getResource('VPureLux');
    var dynamicRows = window.vplDynamicRowSelects;
    var templateAttribute = dynamicRows ? dynamicRows.templateAttribute : 'data-dynamic-row-template';
    var rowSelector = '[data-inventory-line-row]';

    function getLiveRows(container) {
        return container.querySelectorAll(rowSelector + ':not([' + templateAttribute + '])');
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
            dynamicRows.ensureTemplate(container, rowSelector);
            getLiveRows(container).forEach(function (row) {
                dynamicRows.stripSelect2Enhancements(row);
                dynamicRows.initializeSelects(row);
            });
        }

        if (addButton) {
            addButton.addEventListener('click', function () {
                var row;

                if (dynamicRows) {
                    var template = dynamicRows.ensureTemplate(container, rowSelector);

                    if (!template) {
                        return;
                    }

                    row = dynamicRows.createCleanClone(template);
                    row.classList.remove('d-none');
                    row.removeAttribute(templateAttribute);
                    row.removeAttribute('aria-hidden');
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
                    dynamicRows.initializeSelects(row);
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

            removeButton.closest(rowSelector).remove();
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

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-inventory-line-container]').forEach(initializeLineCollection);

        document.querySelectorAll('[data-inventory-posting-page]').forEach(function (page) {
            if (page.dataset.postSuccess) {
                abp.notify.success(page.dataset.postSuccess);
            }

            initializeAdjustmentType(page);
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
}());
