(function () {
    const page = document.querySelector('[data-business-revenue]'); if (!page) return;
    const path = window.location.pathname; const $from = $('#RevenueFromDate'); const $to = $('#RevenueToDate'); const $search = $('#RevenueSearchText');
    const filters = () => ({ fromDate: $from.val(), toDate: $to.val(), searchText: $search.val() });
    const money = value => Number(value || 0).toLocaleString('vi-VN');
    function summary() { abp.ajax({ url: path + '?handler=Summary', type: 'GET', data: filters() }).then(x => { $('#RevenueSales').text(money(x.salesRevenue)); $('#RevenueService').text(money(x.serviceRevenue)); $('#RevenueTotal').text(money(x.totalRevenue)); $('#RevenuePaid').text(money(x.totalPaid)); $('#RevenueRemaining').text(money(x.totalRemaining)); }); }
    const table = $('#BusinessRevenueTable').DataTable(abp.libs.datatables.normalizeConfiguration({ processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [], ajax: abp.libs.datatables.createAjax(input => abp.ajax({ url: path + '?handler=List', type: 'GET', data: input }), filters), columnDefs: [
        { data: null, orderable: false, rowAction: { items: [{ text: abp.localization.getResource('VPureLux')('Details'), action: data => { const x = data.record || data; window.location.href = abp.appPath + (x.source === 'Sales' ? 'Sales/Details/' : 'Service/Details/') + encodeURIComponent(x.documentId); } }] } },
        { data: 'sourceLabel', visible: page.dataset.mode === 'consolidated' }, { data: 'documentNo' }, { data: 'documentDate', className: 'text-nowrap' }, { data: 'customer' }, { data: 'revenue', className: 'text-end' }, { data: 'cost', className: 'text-end' }, { data: 'profit', className: 'text-end' }, { data: 'paid', className: 'text-end' }, { data: 'remaining', className: 'text-end' }
    ] }));
    $('#BusinessRevenueSearchForm').on('submit', event => { event.preventDefault(); table.ajax.reload(); summary(); }); summary();
})();
