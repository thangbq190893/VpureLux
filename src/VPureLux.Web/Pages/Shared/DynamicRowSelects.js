(function (window) {
    var templateAttribute = 'data-dynamic-row-template';
    var select2TargetSelector = 'select[data-use-select2="true"], select.js-select2';

    function getSelectTargets(root, selector) {
        var searchRoot = root || document;
        var targetSelector = selector || select2TargetSelector;

        if (searchRoot.tagName === 'SELECT') {
            return !targetSelector || searchRoot.matches(targetSelector) ? [searchRoot] : [];
        }

        if (typeof searchRoot.querySelectorAll !== 'function') {
            return [];
        }

        return Array.prototype.slice.call(searchRoot.querySelectorAll(targetSelector));
    }

    function stripSelect2Enhancements(root, selector) {
        if (!root) {
            return;
        }

        root.querySelectorAll('.select2-container').forEach(function (node) {
            node.remove();
        });

        root.querySelectorAll('[data-select2-id]').forEach(function (node) {
            node.removeAttribute('data-select2-id');
        });

        getSelectTargets(root, selector).forEach(function (select) {
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

    function stripLeptonXSelectEnhancements(root, selector) {
        getSelectTargets(root, selector).forEach(function (select) {
            select.classList.remove('form-select', 'form-select-sm', 'form-select-lg');
            select.removeAttribute('data-lpx-sync-bound');

            var wrapper = select.closest('.custom-select-wrapper[data-lpx-bound]');

            if (!wrapper) {
                return;
            }

            wrapper.querySelectorAll('.custom-select-display, .custom-options-container').forEach(function (node) {
                node.remove();
            });

            if (wrapper.parentNode) {
                wrapper.parentNode.insertBefore(select, wrapper);
                wrapper.remove();
            }

            select.classList.remove('form-select', 'form-select-sm', 'form-select-lg');
            select.removeAttribute('data-lpx-sync-bound');
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

        var $dropdownParent = $select.closest('.modal, .offcanvas, #SalesCreatePage, #SalesEditPage');

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
            $selects = window.jQuery(root).find(select2TargetSelector);
        }

        stripLeptonXSelectEnhancements(root, selector);

        $selects.each(function () {
            if (this.tagName !== 'SELECT') {
                return;
            }

            var $select = window.jQuery(this);

            if ($select.is('[data-dynamic-select2="disabled"]')) {
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

            this.classList.remove('form-select', 'form-select-sm', 'form-select-lg');

            $select.select2(getSelect2Options($select));
        });
    }

    function createCleanClone(source) {
        var clone = source.cloneNode(true);
        stripSelect2Enhancements(clone);
        stripLeptonXSelectEnhancements(clone);
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
        select2TargetSelector: select2TargetSelector,
        stripSelect2Enhancements: stripSelect2Enhancements,
        stripLeptonXSelectEnhancements: stripLeptonXSelectEnhancements,
        initializeSelects: initializeSelects,
        createCleanClone: createCleanClone,
        ensureTemplate: ensureTemplate,
        setControlsDisabled: setControlsDisabled
    };
}(window));
