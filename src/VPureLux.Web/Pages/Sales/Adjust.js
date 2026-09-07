(function () {
    const page = document.getElementById('SalesAdjustPage');
    const form = document.getElementById('SalesAdjustmentForm');
    if (!page || !form) return;

    const lines = form.querySelector('[data-adjustment-lines]');
    const template = form.querySelector('[data-adjustment-line-template]');

    function reindex() {
        lines.querySelectorAll('[data-adjustment-line]').forEach(function (row, index) {
            row.querySelectorAll('[name]').forEach(function (control) {
                control.name = control.name.replace(/UpdateInput\.Lines\[\d+\]/, 'UpdateInput.Lines[' + index + ']');
            });
        });
    }

    function initProductSelect(select) {
        const $select = $(select);
        if ($select.data('select2')) return;
        $select.select2({
            theme: 'bootstrap-5',
            width: '100%',
            minimumInputLength: 0,
            ajax: {
                url: page.dataset.productLookupUrl,
                dataType: 'json',
                delay: 250,
                data: function (params) { return { term: params.term || '', page: params.page || 1 }; },
                processResults: function (data) { return data; }
            }
        });
        $select.on('change', function () {
            const row = select.closest('[data-adjustment-line]');
            const price = row.querySelector('input[name$=".ActualSellingPrice"]');
            const warning = row.querySelector('[data-product-warning]');
            abp.ajax({
                url: page.dataset.productContextUrl + '&productId=' + encodeURIComponent(select.value),
                type: 'GET'
            }).then(function (context) {
                warning.classList.toggle('d-none', context.hasPublishedBom === true);
                if (context.suggestedPrice !== null && context.suggestedPrice !== undefined) {
                    price.value = context.suggestedPrice;
                }
            });
        });
    }

    lines.querySelectorAll('[data-product-select]').forEach(initProductSelect);

    form.querySelector('[data-add-adjustment-line]').addEventListener('click', function () {
        const index = lines.querySelectorAll('[data-adjustment-line]').length;
        const html = template.innerHTML.replaceAll('__index__', index.toString());
        lines.insertAdjacentHTML('beforeend', html);
        initProductSelect(lines.lastElementChild.querySelector('[data-product-select]'));
    });

    lines.addEventListener('click', function (event) {
        const button = event.target.closest('[data-remove-adjustment-line]');
        if (!button) return;
        const row = button.closest('[data-adjustment-line]');
        const id = row.querySelector('[data-line-id]').value;
        if (id) {
            row.querySelector('[data-line-removed]').value = 'true';
            row.classList.add('d-none');
        } else {
            $(row.querySelector('[data-product-select]')).select2('destroy');
            row.remove();
            reindex();
        }
    });

    form.querySelector('[data-discard-adjustment]').addEventListener('click', function (event) {
        event.preventDefault();
        abp.message.confirm(abp.localization.getResource('VPureLux')('Sales:DiscardAdjustmentConfirm'))
            .then(function (confirmed) {
                if (!confirmed) return;
                form.requestSubmit(event.currentTarget);
            });
    });

})();
