(function () {
    var page = document.querySelector('[data-service-works]');
    if (!page) return;
    var l = abp.localization.getResource('VPureLux');
    var modal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/WorkModal' });
    var encode = function (value) { return $('<div/>').text(value == null ? '' : value).html(); };
    var money = function (value) { return value == null ? encode(l('Service:Unknown')) : encode(Number(value).toLocaleString(abp.localization.currentCulture.name, { maximumFractionDigits: 2 })); };
    var table = $('#ServiceWorksTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true, serverSide: true, paging: true, searching: false, autoWidth: false,
        order: [[1, 'asc']],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({ url: abp.appPath + 'Service/Works?handler=List', type: 'GET', data: input });
        }, function () { return { searchText: $('#ServiceWorkSearch').val(), status: $('#ServiceWorkStatus').val() }; }),
        columnDefs: [
            { data: null, orderable: false, visible: page.dataset.canManage === 'true', rowAction: { items: [
                { text: l('Edit'), action: function (data) { modal.open({ id: (data.record || data).id }); } }
            ] } },
            { data: 'code', render: encode },
            { data: 'name', render: encode },
            { data: 'unit', render: function (value) { return encode(value == null ? l('Service:Unknown') : value); } },
            { data: 'defaultPrice', orderable: false, render: money },
            { data: 'standardCost', orderable: false, render: money },
            { data: 'status', render: function (value) { return encode(l(value === 1 || value === 'Active' ? 'Status:Active' : 'Status:Inactive')); } },
            { data: 'note', orderable: false, render: encode }
        ]
    }));
    modal.onResult(function () { abp.notify.success(l('SavedSuccessfully')); table.ajax.reload(null, false); });
    $('#CreateServiceWork').on('click', function () { modal.open(); });
    $('#ServiceWorkFilters').on('submit', function (event) { event.preventDefault(); table.ajax.reload(); });
    $('#ServiceWorkStatus').on('change', function () { table.ajax.reload(); });
})();
