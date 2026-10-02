// Shared fee-items row logic for Fees Create/Edit (identical markup in both views).
function reindexRows() {
    $('#feeItems tr').each(function (i, tr) {
        $(tr).find('input, select').each(function () {
            var name = $(this).attr('name');
            if (name) {
                $(this).attr('name', name.replace(/Items\[\d+\]/, 'Items[' + i + ']'));
            }
        });
    });
}

var defaultRowHtml = `
    <tr>
        <td>
            <input type="text" name="Items[0].Description" class="form-control form-control-sm" placeholder="Tuition, Lab fee, etc." required />
        </td>
        <td>
            <input type="number" step="0.01" name="Items[0].Amount" class="form-control form-control-sm text-end" value="0.00" required />
        </td>
        <td class="text-center">
            <input type="checkbox" name="Items[0].IsActive" value="true" class="form-check-input m-auto" checked />
        </td>
        <td class="text-center">
            <button type="button" class="btn btn-sm btn-outline-danger removeRow">&times;</button>
        </td>
    </tr>`;

$('#addRow').on('click', function () {
    var $row = $(defaultRowHtml);
    $('#feeItems').append($row);
    reindexRows();
});

$(document).on('click', '.removeRow', function () {
    $(this).closest('tr').remove();
    reindexRows();
});

$('form').on('submit', reindexRows);
