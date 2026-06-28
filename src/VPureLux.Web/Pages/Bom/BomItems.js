(function () {
    var dynamicRows = window.vplDynamicRowSelects;
    var templateAttribute = dynamicRows.templateAttribute;
    var rowSelector = '.bom-item';

    function getLiveRows(container) {
        return container.querySelectorAll(rowSelector + ':not([' + templateAttribute + '])');
    }

    function reindexItems(container) {
        getLiveRows(container).forEach(function (row, index) {
            var component = row.querySelector('.component-id');
            var quantity = row.querySelector('.quantity');

            if (component) {
                component.name = 'Items[' + index + '].ComponentId';
                component.id = 'Items_' + index + '__ComponentId';
            }

            if (quantity) {
                quantity.name = 'Items[' + index + '].Quantity';
                quantity.id = 'Items_' + index + '__Quantity';
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var container = document.getElementById('bom-items');
        var addButton = document.getElementById('add-item');

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
            var sourceComponent = liveRow ? liveRow.querySelector('.component-id') : null;
            var row = dynamicRows.createCleanClone(template);

            row.classList.remove('d-none');
            row.removeAttribute(templateAttribute);
            row.removeAttribute('aria-hidden');

            var component = row.querySelector('.component-id');
            var quantity = row.querySelector('.quantity');

            if (component) {
                if (sourceComponent) {
                    component.innerHTML = sourceComponent.innerHTML;
                }

                component.value = '';
                component.selectedIndex = 0;
            }

            if (quantity) {
                quantity.value = '1';
            }

            container.appendChild(row);
            reindexItems(container);
            dynamicRows.initializeSelects(row);
        });

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('.remove-item');

            if (removeButton && getLiveRows(container).length > 1) {
                removeButton.closest(rowSelector).remove();
                reindexItems(container);
            }
        });
    });
}());
