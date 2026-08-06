(function () {
    const l = abp.localization.getResource('VPureLux');
    const tableSelector = '#AuditTable';

    if (!document.querySelector(tableSelector)) {
        return;
    }

    const $module = $('#AuditModule');
    const $entityType = $('#AuditEntityType');
    const $correlationId = $('#AuditCorrelationId');
    const $severity = $('#AuditSeverity');

    function encode(value) {
        return $('<div/>').text(value || '').html();
    }

    const dataTable = $(tableSelector).DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        searching: false,
        autoWidth: false,
        order: [],
        ajax: abp.libs.datatables.createAjax(function (input) {
            return abp.ajax({
                url: abp.appPath + 'Audit?handler=List',
                type: 'GET',
                data: input
            });
        }, function () {
            return {
                module: $module.val(),
                entityType: $entityType.val(),
                correlationId: $correlationId.val(),
                severity: $severity.val()
            };
        }),
        columnDefs: [
            {
                data: 'eventTime',
                className: 'text-nowrap',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: 'module',
                render: function (data) {
                    return encode(data);
                }
            },
            {
                data: null,
                render: function (_data, _type, row) {
                    return '<span class="fw-semibold">' + encode(row.actionLabel) + '</span>' +
                        '<div class="text-muted small">' + encode(row.eventName) + '</div>';
                }
            },
            {
                data: null,
                render: function (_data, _type, row) {
                    return '<span class="fw-semibold">' + encode(row.entityLabel) + '</span>' +
                        '<div class="text-muted small">' + encode(row.entityType) + '</div>';
                }
            },
            {
                data: null,
                orderable: false,
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.severityBadgeClass) + '">' +
                        encode(row.severityLabel) + '</span>';
                }
            },
            {
                data: null,
                orderable: false,
                render: function (_data, _type, row) {
                    return '<span class="badge ' + encode(row.statusBadgeClass) + '">' +
                        encode(row.statusLabel) + '</span>' +
                        '<div class="text-muted small">' + encode(row.actorTypeLabel) + '</div>';
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                render: function (_data, _type, row) {
                    return '<a class="btn btn-sm btn-outline-secondary" href="' +
                        abp.appPath + 'Audit/Details/' + encodeURIComponent(row.id) + '">' +
                        encode(l('Details')) + '</a>';
                }
            }
        ]
    }));

    $('#AuditSearchForm').on('submit', function (event) {
        event.preventDefault();
        dataTable.ajax.reload();
    });

    $('#AuditClearButton').on('click', function () {
        $module.val('');
        $entityType.val('');
        $correlationId.val('');
        $severity.val('');
        dataTable.ajax.reload();
    });
})();
