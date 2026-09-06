(function () {
    const l = abp.localization.getResource('VPureLux');
    const page = document.querySelector('[data-service-order-form]');
    if (!page) return;

    const $asset = $('#ServiceAssetId');
    const $technician = $('#ServiceTechnicianId');
    const endpoint = window.location.pathname;
    let positions = [];

    function stripEnhancements(element) {
        if (window.vplDynamicRowSelects) {
            window.vplDynamicRowSelects.stripLeptonXSelectEnhancements(element);
        }
        $(element).next('.select2-container').remove();
        $(element).removeClass('select2-hidden-accessible')
            .removeAttr('data-select2-id aria-hidden tabindex');
    }

    function initRemoteSelect($select, handler, allowClear) {
        if (!$select.length || $select.hasClass('d-none')) return;
        if ($select.data('select2')) $select.select2('destroy');
        stripEnhancements($select[0]);
        $select.select2({
            theme: 'bootstrap-5',
            width: '100%',
            allowClear: allowClear,
            placeholder: $select.data('placeholder') || l('Select'),
            minimumInputLength: 0,
            ajax: {
                url: endpoint + '?handler=' + handler,
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { searchText: params.term || '', page: params.page || 1 };
                },
                processResults: function (data) {
                    return data;
                }
            }
        });
    }

    function loadPositions(assetId) {
        positions = [];
        if (!assetId) {
            refreshAllPositionSelects(true);
            return;
        }
        abp.ajax({
            url: endpoint + '?handler=Positions&assetId=' + encodeURIComponent(assetId),
            type: 'GET'
        }).then(function (data) {
            positions = data || [];
            refreshAllPositionSelects(false);
        });
    }

    function refreshAllPositionSelects(clearSelection) {
        document.querySelectorAll('[data-service-line]').forEach(function (row) {
            refreshPositionSelect(row, clearSelection);
        });
    }

    function refreshPositionSelect(row, clearSelection) {
        const select = row.querySelector('.service-position-select');
        if (!select) return;
        const labelInput = row.querySelector('.service-position-label');
        const isMaterial = row.dataset.lineType === 'Material';
        const selectedId = clearSelection ? '' : select.value;
        const selectedText = clearSelection ? '' : (labelInput?.value || select.selectedOptions[0]?.text || '');
        const componentId = row.querySelector('.service-item-select')?.value;

        select.innerHTML = '';
        select.add(new Option(l('Service:NoPosition'), ''));
        positions.filter(function (item) {
            return !componentId || !item.componentId || String(item.componentId).toLowerCase() === String(componentId).toLowerCase();
        }).forEach(function (item) {
            select.add(new Option(item.text, item.id, false, String(item.id) === String(selectedId)));
        });
        if (selectedId && !Array.from(select.options).some(option => String(option.value) === String(selectedId))) {
            select.add(new Option(selectedText || selectedId, selectedId, true, true));
        }
        select.disabled = !isMaterial;
        if (!isMaterial || clearSelection) {
            select.value = '';
            if (labelInput) labelInput.value = '';
        }
    }

    function initItemSelect(row) {
        const $select = $(row).find('.service-item-select');
        const handler = row.dataset.lineType === 'Material' ? 'Materials' : 'Works';
        initRemoteSelect($select, handler, false);
        $select.off('.serviceLine').on('select2:select.serviceLine', function (event) {
            const item = event.params.data;
            const priceInput = row.querySelector('.service-price');
            const labelInput = row.querySelector('.service-item-label');
            if (item.price !== undefined && item.price !== null && priceInput) priceInput.value = item.price;
            if (labelInput) labelInput.value = item.text || '';
            refreshPositionSelect(row, true);
        }).on('select2:clear.serviceLine', function () {
            const labelInput = row.querySelector('.service-item-label');
            if (labelInput) labelInput.value = '';
            refreshPositionSelect(row, true);
        });
    }

    function renumber() {
        document.querySelectorAll('[data-service-line]').forEach(function (row, index) {
            row.querySelectorAll('[data-name]').forEach(function (input) {
                input.name = 'Input.Lines[' + index + '].' + input.dataset.name;
                input.disabled = input.classList.contains('service-position-select') && row.dataset.lineType !== 'Material';
            });
            ['Id', 'LineType', 'CatalogItemId', 'CustomerAssetComponentId', 'Quantity', 'UnitPrice', 'Note', 'ItemLabel', 'PositionLabel']
                .forEach(function (name) {
                    const input = row.querySelector('[name$=".' + name + '"]');
                    if (input && !(input.classList.contains('service-position-select') && row.dataset.lineType !== 'Material')) {
                        input.disabled = false;
                    }
                });
        });
        $('#ServiceNoLines').toggleClass('d-none', document.querySelectorAll('[data-service-line]').length > 0);
    }

    document.querySelectorAll('[data-service-line]').forEach(function (row) {
        initItemSelect(row);
        row.querySelector('.service-position-select')?.addEventListener('change', function (event) {
            const label = event.target.selectedOptions[0]?.text || '';
            const labelInput = row.querySelector('.service-position-label');
            if (labelInput) labelInput.value = event.target.value ? label : '';
        });
    });

    document.querySelectorAll('[data-add-service-line]').forEach(function (button) {
        button.addEventListener('click', function () {
            const type = button.dataset.addServiceLine;
            const badge = type === 'Material' ? 'text-bg-primary' : 'text-bg-secondary';
            const html = document.getElementById('ServiceLineTemplate').innerHTML
                .replaceAll('__TYPE__', type)
                .replaceAll('__BADGE__', badge)
                .replaceAll('__TYPE_LABEL__', l('Service:LineType:' + type));
            const holder = document.createElement('tbody');
            holder.innerHTML = html.trim();
            const row = holder.firstElementChild;
            document.querySelector('[data-service-lines-body]').appendChild(row);
            renumber();
            initItemSelect(row);
            refreshPositionSelect(row, true);
            row.querySelector('.service-position-select')?.addEventListener('change', function (event) {
                row.querySelector('.service-position-label').value = event.target.value
                    ? event.target.selectedOptions[0]?.text || ''
                    : '';
            });
        });
    });

    page.addEventListener('click', function (event) {
        const button = event.target.closest('[data-remove-service-line]');
        if (!button) return;
        const row = button.closest('[data-service-line]');
        const $select = $(row).find('.service-item-select');
        if ($select.data('select2')) $select.select2('destroy');
        row.remove();
        renumber();
    });

    if (page.dataset.editMode !== 'true') {
        initRemoteSelect($asset, 'Assets', false);
        $asset.on('select2:select', function (event) {
            const item = event.params.data;
            $('#ServiceAssetLabel').val(item.text || '');
            if (!$('#ServiceAddress').val() && item.address) $('#ServiceAddress').val(item.address);
            loadPositions(item.id);
        }).on('select2:clear', function () {
            $('#ServiceAssetLabel').val('');
            loadPositions(null);
        });
    }

    initRemoteSelect($technician, 'Technicians', true);
    $technician.on('select2:select', function (event) {
        $('#ServiceTechnicianLabel').val(event.params.data.text || '');
    }).on('select2:clear', function () {
        $('#ServiceTechnicianLabel').val('');
    });

    if ($asset.val()) loadPositions($asset.val());
    renumber();
})();
