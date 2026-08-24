(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-service-edit-form]');
    if (!page) return;
    const $asset = $('#ServiceAssetId');
    let positions = [];

    function ajaxSelect($select, handler) {
        if (window.vplDynamicRowSelects) {
            window.vplDynamicRowSelects.stripLeptonXSelectEnhancements($select[0]);
        }
        $select.select2({
            theme: 'bootstrap-5', width: '100%', allowClear: true, placeholder: $select.data('placeholder') || l('Select'), minimumInputLength: 0,
            ajax: { url: window.location.pathname + '?handler=' + handler, dataType: 'json', delay: 250,
                data: params => ({ searchText: params.term || '' }), processResults: data => data }
        });
    }
    if (!$asset.hasClass('d-none')) ajaxSelect($asset, 'Assets');

    function loadPositions(assetId) {
        positions = [];
        if (!assetId) { refreshPositionSelects(); return; }
        abp.ajax({ url: window.location.pathname + '?handler=Positions&assetId=' + encodeURIComponent(assetId), type: 'GET' })
            .then(data => { positions = data || []; refreshPositionSelects(); });
    }
    function refreshPositionSelects() {
        document.querySelectorAll('.service-position-select').forEach(select => {
            const selected = select.value;
            select.innerHTML = '<option value="">' + $('<div/>').text(l('Service:SelectPosition')).html() + '</option>';
            positions.forEach(item => select.add(new Option(item.text, item.id, false, item.id === selected)));
            select.disabled = select.closest('[data-service-line]').dataset.lineType !== 'Material';
        });
    }
    function initItemSelect(row) {
        const $select = $(row).find('.service-item-select');
        if ($select.data('select2')) $select.select2('destroy');
        const handler = row.dataset.lineType === 'Material' ? 'Materials' : 'Works';
        ajaxSelect($select, handler);
        $select.on('select2:select', event => {
            const price = event.params.data.price;
            if (price !== undefined) row.querySelector('.service-price').value = price;
        });
    }
    function renumber() {
        document.querySelectorAll('[data-service-line]').forEach((row, index) => {
            row.querySelectorAll('[data-name]').forEach(input => input.name = 'Input.Lines[' + index + '].' + input.dataset.name);
            const known = ['LineType', 'CatalogItemId', 'CustomerAssetComponentId', 'Quantity', 'UnitPrice'];
            known.forEach(name => {
                const input = row.querySelector('[name$=".' + name + '"]');
                if (input) input.name = 'Input.Lines[' + index + '].' + name;
            });
        });
        $('#ServiceNoLines').toggleClass('d-none', document.querySelectorAll('[data-service-line]').length > 0);
    }
    document.querySelectorAll('[data-service-line]').forEach(row => initItemSelect(row));
    document.querySelectorAll('[data-add-service-line]').forEach(button => button.addEventListener('click', () => {
        const type = button.dataset.addServiceLine;
        const template = document.getElementById('ServiceLineTemplate').innerHTML
            .replaceAll('__TYPE__', type).replaceAll('__TYPE_LABEL__', l('Service:' + type));
        const holder = document.createElement('tbody'); holder.innerHTML = template.trim();
        const row = holder.firstElementChild; document.querySelector('#ServiceLinesTable tbody').appendChild(row);
        initItemSelect(row); refreshPositionSelects(); renumber();
    }));
    page.addEventListener('click', event => {
        const button = event.target.closest('[data-remove-service-line]');
        if (!button) return;
        const row = button.closest('[data-service-line]');
        const $select = $(row).find('.service-item-select'); if ($select.data('select2')) $select.select2('destroy');
        row.remove(); renumber();
    });
    $asset.on('select2:select', event => {
        const item = event.params.data;
        if (!$('#ServiceAddress').val() && item.address) $('#ServiceAddress').val(item.address);
        loadPositions(item.id);
    }).on('select2:clear', () => loadPositions(null));
    if ($asset.val()) loadPositions($asset.val());
    renumber();
})();
