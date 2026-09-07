(function () {
    var root = document.getElementById('BusinessReport');
    if (!root) return;
    var l = abp.localization.getResource('VPureLux');
    var enc = function (v) { return $('<div/>').text(v == null ? '' : v).html(); };
    var money = function (v) { return v == null ? '-' : enc(Number(v).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' \u20ab'); };
    var filters = function () { return { source: $('#ReportSource').val() || null, fromDateText: $('#ReportFrom').val(), toDateText: $('#ReportTo').val(), searchText: $('#ReportSearch').val() }; };
    var endpoint = window.location.pathname;
    var summaryVersion = 0;
    const summary = () => {
        var version = ++summaryVersion;
        $('#ReportTotals dd').text('-');
        return abp.ajax({ url: endpoint + '?handler=Summary', type: 'GET', data: filters() }).done(function (data) {
            if (version !== summaryVersion) return;
            $('#ReportTotals [data-total]').each(function () {
                var key = this.dataset.total, value = data[key];
                $(this).text(key === 'profit' && value == null && data.costIncompleteCount > 0 ? l('Reports:UnknownCost') :
                    value == null ? '-' : key.toLowerCase().includes('count') ? Number(value).toLocaleString('vi-VN') :
                        Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' \u20ab');
            });
        });
    };
    var table = $('#BusinessReportTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        serverSide: true, processing: true, paging: true, searching: false, autoWidth: false, order: [[1, 'desc']],
        ajax: abp.libs.datatables.createAjax(function (input) { return abp.ajax({ url: endpoint + '?handler=List', type: 'GET', data: input }); }, filters),
        columnDefs: [
            { data: 'source', orderable: false, visible: root.dataset.serviceOnly !== 'true', render: function (v) { return enc(l('Reports:Source:' + (v === 1 ? 'Sales' : 'Service'))); } },
            { data: 'documentDate', className: 'text-wrap', render: function (v) {
                return enc(v.slice(8, 10) + '/' + v.slice(5, 7) + '/' + v.slice(0, 4) + ' ' + v.slice(11, 16));
            } },
            { data: 'documentNo', render: function (v, t, r) {
                var link = r.source === 1 ? root.dataset.linkSales === 'true' : root.dataset.linkService === 'true';
                return link ? '<a href="' + abp.appPath + (r.source === 1 ? 'Sales/Details/' : 'Service/Details/') + encodeURIComponent(r.documentId) + '">' + enc(v) + '</a>' : enc(v);
            } },
            { data: 'customerName', render: function (v, t, r) { return enc(r.customerCode + ' - ' + v); } },
            { data: 'assetName', orderable: false, render: function (v, t, r) { return enc(r.assetNo ? r.assetNo + ' - ' + v : '-'); } },
            { data: 'revenue', render: money },
            { data: 'materialCost', visible: root.dataset.cost === 'true' && root.dataset.serviceOnly === 'true', orderable: false, render: money },
            { data: 'laborCost', visible: root.dataset.cost === 'true' && root.dataset.serviceOnly === 'true', orderable: false,
                render: function (v, t, r) { return r.laborCostKnown === false ? enc(l('Reports:UnknownCost')) : money(v); } },
            { data: 'totalKnownCost', visible: root.dataset.cost === 'true', render: function (v, t, r) {
                return money(v) + (r.costIncomplete === true ? ' <span class="text-warning" title="' + enc(l('Reports:UnknownCost')) + '">*</span>' : '');
            } },
            { data: 'profit', visible: root.dataset.profit === 'true', render: function (v, t, r) { return v == null && r.costIncomplete === true ? enc(l('Reports:UnknownCost')) : money(v); } },
            { data: 'grossPosted', orderable: false, render: money },
            { data: 'grossRefunded', orderable: false, render: money },
            { data: 'netPaid', orderable: false, render: function (v, t, r) { return r.hasInconsistentLedger ? enc(l('Reports:LedgerInvalid')) : money(v); } },
            { data: 'receivable', orderable: false, render: money },
            { data: 'refundDue', orderable: false, render: money }
        ]
    }));
    $('#BusinessReportFilters').on('submit', function (e) { e.preventDefault(); table.ajax.reload(); summary(); });
    summary();
})();
