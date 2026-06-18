(function () {
    var l = abp.localization.getResource('VPureLux');

    function applyTemplate(element, attributeName, index) {
        var template = element.getAttribute(attributeName);

        if (template) {
            element.setAttribute(attributeName.replace('data-', ''), template.replace(/__index__/g, index));
        }
    }

    function reindexRows(container) {
        container.querySelectorAll('[data-inventory-line-row]').forEach(function (row, index) {
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

        if (addButton) {
            addButton.addEventListener('click', function () {
                var source = container.querySelector('[data-inventory-line-row]');

                if (!source) {
                    return;
                }

                var row = source.cloneNode(true);
                clearRow(row);
                container.appendChild(row);
                reindexRows(container);
            });
        }

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('[data-remove-line]');

            if (!removeButton) {
                return;
            }

            var rows = container.querySelectorAll('[data-inventory-line-row]');
            if (rows.length <= 1) {
                return;
            }

            removeButton.closest('[data-inventory-line-row]').remove();
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
