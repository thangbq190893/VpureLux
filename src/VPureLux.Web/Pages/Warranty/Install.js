(function () {
    const page = document.querySelector('[data-warranty-installation]');
    const body = document.getElementById('InstallationPositionsBody');
    const template = document.getElementById('InstallationPositionTemplate');

    if (!page || !body || !template) {
        return;
    }

    function initializeSelect($select) {
        if ($select.data('select2')) {
            return;
        }

        $select.select2({ width: '100%' });
    }

    function renumberRows() {
        body.querySelectorAll('[data-installation-position]').forEach(function (row, index) {
            row.querySelectorAll('[name]').forEach(function (input) {
                input.name = input.name.replace(/Input\.Positions\[\d+\]/, 'Input.Positions[' + index + ']');
            });
        });
    }

    $(body).find('.js-installation-component').each(function () {
        initializeSelect($(this));
    });

    document.getElementById('AddInstallationPosition').addEventListener('click', function () {
        const index = body.querySelectorAll('[data-installation-position]').length;
        const fragment = template.content.cloneNode(true);
        fragment.querySelectorAll('[name]').forEach(function (input) {
            input.name = input.name.replaceAll('__index__', index);
        });
        body.appendChild(fragment);
        initializeSelect($(body.lastElementChild).find('.js-installation-component'));
    });

    body.addEventListener('click', function (event) {
        const button = event.target.closest('[data-remove-installation-position]');
        if (!button) {
            return;
        }

        const row = button.closest('[data-installation-position]');
        const $select = $(row).find('.js-installation-component');
        if ($select.data('select2')) {
            $select.select2('destroy');
        }
        row.remove();
        renumberRows();
    });
})();
