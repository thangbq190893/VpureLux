(function () {
    if (window.vpureluxReplacementPolicyFieldsInitialized) {
        return;
    }

    window.vpureluxReplacementPolicyFieldsInitialized = true;

    function update(editor) {
        const toggle = editor.querySelector(
            'input[type="checkbox"][name$=".ReplacementPolicy.IsEnabled"]');
        const fields = editor.querySelector('[data-replacement-policy-fields]');

        if (!toggle || !fields) {
            return;
        }

        fields.classList.toggle('d-none', !toggle.checked);
    }

    function initialize(root) {
        root.querySelectorAll('[data-replacement-policy-editor]').forEach(update);
    }

    document.addEventListener('change', function (event) {
        if (!event.target.matches(
            'input[type="checkbox"][name$=".ReplacementPolicy.IsEnabled"]')) {
            return;
        }

        const editor = event.target.closest('[data-replacement-policy-editor]');
        if (editor) {
            update(editor);
        }
    });

    initialize(document);
})();
