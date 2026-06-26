(function (window) {
    var templateAttribute = 'data-dynamic-row-template';

    function stripSelect2Enhancements(root) {
        if (!root) {
            return;
        }

        root.querySelectorAll('.select2-container').forEach(function (node) {
            node.remove();
        });

        root.querySelectorAll('select.form-select').forEach(function (select) {
            if (window.jQuery) {
                var $select = window.jQuery(select);

                if ($select.data('select2')) {
                    try {
                        $select.select2('destroy');
                    } catch (error) {
                        // Ignore destroy failures on partially initialized clones.
                    }
                }
            }

            select.classList.remove('select2-hidden-accessible');
            select.removeAttribute('data-select2-id');
            select.removeAttribute('aria-hidden');
            select.removeAttribute('tabindex');
            select.style.display = '';
        });
    }

    function getSelect2Options($select) {
        var options = {
            theme: 'bootstrap-5',
            width: '100%'
        };

        var $dropdownParent = $select.closest('.modal, .offcanvas');
        if ($dropdownParent.length) {
            options.dropdownParent = $dropdownParent;
        }

        return options;
    }

    function initializeSelects(root) {
        if (!window.jQuery || !window.jQuery.fn.select2) {
            return;
        }

        window.jQuery(root).find('select.form-select').each(function () {
            var $select = window.jQuery(this);

            if ($select.hasClass('auto-complete-select')) {
                if (window.abp &&
                    abp.dom &&
                    abp.dom.initializers &&
                    typeof abp.dom.initializers.initializeAutocompleteSelects === 'function') {
                    abp.dom.initializers.initializeAutocompleteSelects($select);
                }

                return;
            }

            if ($select.data('select2')) {
                return;
            }

            $select.select2(getSelect2Options($select));
        });
    }

    function createCleanClone(source) {
        var clone = source.cloneNode(true);
        stripSelect2Enhancements(clone);
        return clone;
    }

    function ensureTemplate(container, rowSelector) {
        var template = container.querySelector('[' + templateAttribute + ']');

        if (template) {
            return template;
        }

        var source = container.querySelector(rowSelector + ':not([' + templateAttribute + '])');

        if (!source) {
            return null;
        }

        template = createCleanClone(source);
        template.setAttribute(templateAttribute, '');
        template.classList.add('d-none');
        template.setAttribute('aria-hidden', 'true');
        container.appendChild(template);

        return template;
    }

    window.vplDynamicRowSelects = {
        templateAttribute: templateAttribute,
        stripSelect2Enhancements: stripSelect2Enhancements,
        initializeSelects: initializeSelects,
        createCleanClone: createCleanClone,
        ensureTemplate: ensureTemplate
    };
}(window));
