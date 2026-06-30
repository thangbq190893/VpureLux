(function (window) {
    var templateAttribute = 'data-dynamic-row-template';

    function stripSelect2Enhancements(root) {
        if (!root) {
            return;
        }

        root.querySelectorAll('.select2-container').forEach(function (node) {
            node.remove();
        });

        var selects = root.tagName === 'SELECT'
            ? [root]
            : Array.prototype.slice.call(root.querySelectorAll('select.form-select, select[data-sales-product-select]'));

        selects.forEach(function (select) {
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

    function setControlsDisabled(root, disabled) {
        if (!root) {
            return;
        }

        root.querySelectorAll('input, select, textarea, button').forEach(function (element) {
            element.disabled = disabled;
        });
    }

    function getSelect2Options($select) {
        var options = {
            theme: 'bootstrap-5',
            width: '100%'
        };

        var $dropdownParent = $select.closest('.modal, .offcanvas, #SalesCreatePage, form');

        if ($dropdownParent.length) {
            options.dropdownParent = $dropdownParent;
        }

        return options;
    }

    function initializeSelects(root, selector) {
        if (!window.jQuery || !window.jQuery.fn.select2 || !root) {
            return;
        }

        var $selects;

        if (root.tagName === 'SELECT') {
            $selects = window.jQuery(root);
        } else if (selector) {
            $selects = window.jQuery(root).find(selector);
        } else {
            $selects = window.jQuery(root).find('select.form-select');
        }

        $selects.each(function () {
            var $select = window.jQuery(this);

            if ($select.is('[data-sales-product-select]') && root.tagName !== 'SELECT') {
                return;
            }

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

            if (!$select.hasClass('form-select')) {
                $select.addClass('form-select');
            }

            $select.select2(getSelect2Options($select));
        });
    }

    function createCleanClone(source) {
        var clone = source.cloneNode(true);
        stripSelect2Enhancements(clone);
        setControlsDisabled(clone, false);
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
        setControlsDisabled(template, true);
        container.appendChild(template);

        return template;
    }

    window.vplDynamicRowSelects = {
        templateAttribute: templateAttribute,
        stripSelect2Enhancements: stripSelect2Enhancements,
        initializeSelects: initializeSelects,
        createCleanClone: createCleanClone,
        ensureTemplate: ensureTemplate,
        setControlsDisabled: setControlsDisabled
    };
}(window));
