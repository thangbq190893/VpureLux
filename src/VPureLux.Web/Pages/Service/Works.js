(function () {
    const l = abp.localization.getResource('VPureLux'); const page = document.querySelector('[data-service-works]'); if (!page) return;
    const modal = new abp.ModalManager({ viewUrl: abp.appPath + 'Service/WorkModal' });
    const token = $('#ServiceWorksTokenForm').find('input[name="__RequestVerificationToken"]').val();
    const tokenHeaders = token ? { RequestVerificationToken: token } : {};
    const table = $('#ServiceWorksTable').DataTable(abp.libs.datatables.normalizeConfiguration({ processing: true, serverSide: true, paging: true, searching: false, autoWidth: false, order: [], ajax: abp.libs.datatables.createAjax(input => abp.ajax({ url: abp.appPath + 'Service/Works?handler=List', type: 'GET', data: input })), columnDefs: [
        { data: null, orderable: false, rowAction: { items: [ { text: l('Edit'), action: data => modal.open({ id: (data.record || data).id }) }, { text: l('Deactivate'), visible: data => (data.record || data).isActive, action: data => { const row = data.record || data; abp.ajax({ url: abp.appPath + 'Service/Works?handler=SetActive&id=' + encodeURIComponent(row.id) + '&isActive=false', type: 'POST', headers: tokenHeaders }).then(() => table.ajax.reload(null, false)); } }, { text: l('Activate'), visible: data => !(data.record || data).isActive, action: data => { const row = data.record || data; abp.ajax({ url: abp.appPath + 'Service/Works?handler=SetActive&id=' + encodeURIComponent(row.id) + '&isActive=true', type: 'POST', headers: tokenHeaders }).then(() => table.ajax.reload(null, false)); } } ] } },
        { data: 'code' }, { data: 'name' }, { data: 'defaultPrice', className: 'text-end' }, { data: 'status' }
    ] }));
    modal.onResult(() => table.ajax.reload(null, false)); document.getElementById('CreateServiceWork').addEventListener('click', () => modal.open());
})();
