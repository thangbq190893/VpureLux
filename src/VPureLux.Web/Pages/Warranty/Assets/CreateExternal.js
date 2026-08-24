(function () {
    const page = document.querySelector('[data-external-asset-editor]');
    const body = document.getElementById('ExternalAssetPositionsBody');
    const template = document.getElementById('ExternalAssetPositionTemplate');
    if (!page || !body || !template) return;

    const initSelect = select => { const $select = $(select); if (!$select.data('select2')) $select.select2({ width: '100%' }); };
    page.querySelectorAll('.js-external-asset-select').forEach(initSelect);

    function renumber() {
        body.querySelectorAll('[data-external-position]').forEach((row, index) => {
            row.querySelectorAll('[name]').forEach(input => {
                input.name = input.name.replace(/Input\.Positions\[\d+\]/, 'Input.Positions[' + index + ']');
            });
        });
    }

    document.getElementById('AddExternalAssetPosition').addEventListener('click', () => {
        const index = body.querySelectorAll('[data-external-position]').length;
        const fragment = template.content.cloneNode(true);
        fragment.querySelectorAll('[name]').forEach(input => { input.name = input.name.replaceAll('__index__', index); });
        body.appendChild(fragment);
        initSelect(body.lastElementChild.querySelector('.js-external-asset-select'));
    });

    body.addEventListener('click', event => {
        const button = event.target.closest('[data-remove-external-position]');
        if (!button) return;
        const row = button.closest('[data-external-position]');
        const $select = $(row).find('.js-external-asset-select');
        if ($select.data('select2')) $select.select2('destroy');
        row.remove();
        renumber();
    });
})();
