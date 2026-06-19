(function () {
    function reindexItems() {
        document.querySelectorAll('.bom-item').forEach(function (row, index) {
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

        if (!container || !addButton) {
            return;
        }

        addButton.addEventListener('click', function () {
            var source = container.querySelector('.bom-item');

            if (!source) {
                return;
            }

            var sourceComponent = source.querySelector('.component-id');
            var row = source.cloneNode(true);
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
            reindexItems();
        });

        container.addEventListener('click', function (event) {
            var removeButton = event.target.closest('.remove-item');

            if (removeButton && container.querySelectorAll('.bom-item').length > 1) {
                removeButton.closest('.bom-item').remove();
                reindexItems();
            }
        });
    });
}());
