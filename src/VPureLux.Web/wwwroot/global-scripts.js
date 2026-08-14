/* Your Global Scripts */
(function () {
    const displayDatePattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/;
    const isoDatePattern = /^(\d{4})-(\d{2})-(\d{2})$/;

    function pad2(value) {
        return value.toString().padStart(2, '0');
    }

    function toDisplayDate(value) {
        if (!value) {
            return '';
        }

        const match = isoDatePattern.exec(value.trim());
        if (!match) {
            return value;
        }

        return parseInt(match[3], 10) + '/' + parseInt(match[2], 10) + '/' + match[1];
    }

    function toIsoDate(value) {
        if (!value) {
            return '';
        }

        const trimmed = value.trim();
        if (isoDatePattern.test(trimmed)) {
            return trimmed;
        }

        const match = displayDatePattern.exec(trimmed);
        if (!match) {
            return trimmed;
        }

        const day = parseInt(match[1], 10);
        const month = parseInt(match[2], 10);
        const year = parseInt(match[3], 10);

        if (day < 1 || day > 31 || month < 1 || month > 12) {
            return trimmed;
        }

        return year + '-' + pad2(month) + '-' + pad2(day);
    }

    function configureBootstrapDatepicker() {
        if (!window.jQuery || !$.fn || !$.fn.datepicker) {
            return;
        }

        $.extend($.fn.datepicker.defaults, {
            autoclose: true,
            format: 'dd/mm/yyyy',
            language: 'vi',
            todayHighlight: true,
            weekStart: 1
        });
    }

    function normalizeDateInput(input) {
        const $input = $(input);
        const rawValue = $input.val();

        try {
            input.type = 'text';
        } catch (_ignored) {
            $input.attr('type', 'text');
        }

        $input
            .addClass('vpl-date-input')
            .attr('autocomplete', 'off')
            .attr('inputmode', 'numeric')
            .attr('placeholder', 'dd/MM/yyyy')
            .val(toDisplayDate(rawValue));

        if ($.fn && $.fn.datepicker) {
            if ($input.data('datepicker')) {
                $input.datepicker('destroy');
            }

            $input.datepicker({
                autoclose: true,
                format: 'dd/mm/yyyy',
                language: 'vi',
                todayHighlight: true,
                weekStart: 1
            });
        }
    }

    function normalizeDateInputs() {
        configureBootstrapDatepicker();
        $('input[type="date"], input.vpl-date-input').each(function () {
            normalizeDateInput(this);
        });
    }

    window.vPureLuxDate = {
        normalizeInputs: normalizeDateInputs,
        toDisplay: toDisplayDate,
        toIso: toIsoDate
    };

    configureBootstrapDatepicker();

    $(function () {
        normalizeDateInputs();
    });
})();
