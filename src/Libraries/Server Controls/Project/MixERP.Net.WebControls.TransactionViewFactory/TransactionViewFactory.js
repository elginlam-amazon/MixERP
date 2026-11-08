var transactionGridView = $("#TransactionGridView");
var printButton = $("#PrintButton");

$(document).ready(function () {
    initTransactionView();
});

//Resolves the grid FRESH on every call.
//
//Blazor re-renders MixERPGridView's <table> whenever the data source changes (and its
//empty-data table and its populated table are two different elements), so the module-level
//"transactionGridView" captured when this script was parsed quickly points at a DETACHED
//node. Every selection/print/flag lookup must therefore re-resolve the grid by id instead
//of reusing that cached jQuery object.
var getTransactionGridView = function () {
    transactionGridView = $("#TransactionGridView");
    return transactionGridView;
};

var getPrintButton = function () {
    printButton = $("#PrintButton");
    return printButton;
};

var printTransactionGridView = function () {
    var templatePath = "/Reports/Print.html";
    var headerPath = "/Reports/Assets/Header.aspx";
    var title = $("h1").html();
    var targetControlId = "TransactionGridView";
    var date = now;
    var windowName = "Journal";
    var offsetFirst = 2;
    var offsetLast = 2;

    printGridView(templatePath, headerPath, title, targetControlId, date, user, office, windowName, offsetFirst, offsetLast);
};

var updateFlagColor = function () {
    var grid = getTransactionGridView();
    createFlaggedRows(grid);
};

var updatePreviewButtons = function () {
    var grid = getTransactionGridView();

    grid.find("tbody tr").each(function () {
        var row = $(this);
        var book = row.find("td:nth-child(5)").html();
        var glReportIcon = row.find("td:first-child").find(".icon.print");
        var icon = row.find("td:first-child").find(".icon.grid.layout");
        if (!
     (book.substring(0, 5).toLowerCase() === "sales"
         || book.substring(0, 8).toLowerCase() === "purchase")) {
            icon.hide();
        }
        if (book.substring(0, 9).toLowerCase() === "inventory" || book.toLowerCase() === "opening.inventory") {
            icon.show();
            glReportIcon.hide();
        }
        if (book.substring(0, 16).toLowerCase() === "purchase.payment" || book.substring(0, 13).toLowerCase() === "sales.receipt") {
            icon.hide();
        }
    });
};

var getSelectedItems = function () {
    //Set the position of the column which contains the checkbox.
    var checkBoxColumnPosition = "2";

    //Set the position of the column which contains id.
    var idColumnPosition = "3";

    var selection = getSelectedCheckBoxItemIds(checkBoxColumnPosition, idColumnPosition, getTransactionGridView());

    if (selection.length > 0) {
        $("#SelectedValuesHidden").val(selection.join(','));
        return true;
    } else {
        $.notify(Resources.Titles.NothingSelected(), "error");
        return false;
    }
};

//(Re)wires everything this script owns against the CURRENT DOM.
//
//IDEMPOTENT by construction: every binding is namespaced and re-bound with
//.off('click.transactionView').on('click.transactionView', ...), and no jQuery object is
//cached across calls - so repeated calls REBIND rather than stack handlers. The component
//calls this after EVERY render (not just the first), because Blazor replaces the grid's
//<table>/<tr> set on every data change (initial load, Show, a flag update) while this
//script element is injected only once per circuit. Without it the row-click selection,
//the print button and the flag row colours render and silently do nothing.
window.initTransactionView = function () {
    var grid = getTransactionGridView();

    grid.find('tr').off('click.transactionView').on('click.transactionView', function () {
        var checkBox = $(this).find('td input:checkbox');
        toogleSelection(checkBox);
    });

    getPrintButton().off('click.transactionView').on('click.transactionView', function () {
        printTransactionGridView();
    });

    updateFlagColor();
    updatePreviewButtons();
};

function showCheckList(element) {
    var current = parseFloat2($(element).parent().parent().find("td:nth-child(3)").html());
    var url = "Confirmation/JournalVoucher.mix?TranId=" + current;

    window.location = url;
};

function showPreview(element) {
    var current = parseFloat2($(element).parent().parent().find("td:nth-child(3)").html());
    showWindow('Reports/GLAdviceReport.mix?TranId=' + current);
    return false;
};

function showStockDetail(element) {
    var current = parseFloat2($(element).parent().parent().find("td:nth-child(3)").html());
    showWindow('Reports/StockTransactionReport.mix?TranId=' + current);
    return false;
};
