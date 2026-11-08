/*
    Profit-and-loss screen controller for Modules/Finance/Reports/ProfitAndLossAccount.

    EXTRACTED VERBATIM from the inline <script type="text/javascript"> block of
    Modules/Finance/Reports/ProfitAndLossAccount.ascx. It had to leave the markup: Blazor STRIPS
    <script> elements inside a .razor file, so the ported component loads this file instead, on
    the render AFTER the asynchronous query - everything below reads the rendered
    #PLAccountGridView out of the DOM.

    POSITIONAL GRID CONTRACT:
      td:last / th:last   read TWICE per row - first the "is summation" flag (row marked
                          `positive` + `strong` when its input is :checked, then the column is
                          REMOVED), then the "is profit" flag (row marked `negative`, column
                          hidden).
      td:first-child      indented for rows that got neither class.

    ⚠ PRE-EXISTING BEHAVIOUR, PRESERVED: the second ("is profit") cell is EMPTY in the rendered
    grid, because the code-behind's RowDataBound assigned .Text to cells 1..Count-2 and
    assigning TableCell.Text clears that cell's controls - destroying its checkbox. So the
    `negative` branch below has never matched. It is kept as found; making it match would newly
    colour rows. See the ported component's header comment.

    compactCheckBox.change is the ONLY writer of #IsCompactHidden, which the ported server-side
    Show handler reads back through the module's interop reader.
*/

var grid = $("#PLAccountGridView");
var compactCheckBox = $("#CompactCheckBox");
var isCompactHiddenField = $("#IsCompactHidden");

$(document).ready(function () {
    grid.find("tr").each(function () {
        var isSummation = $(this).find("td:last, th:last");

        if (isSummation.find("input").is(":checked") === true) {
            $(this).addClass("positive");
            $(this).find("td").addClass("strong");
        };

        isSummation.remove();

        var isProfit = $(this).find("td:last, th:last");

        if (isProfit.find("input").is(":checked") === true) {
            $(this).addClass("negative");
            $(this).find("td").addClass("strong");
        };
        isProfit.hide();
    });

    grid.find("tr").each(function () {
        if ($(this).is(".positive, .negative") === false) {
            var cell = $(this).find("td:first-child");
            cell.html("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cell.html());
        };
    });

});

compactCheckBox.change(function () {
    if (compactCheckBox.is(":checked")) {
        isCompactHiddenField.val("1");
        return;
    };

    isCompactHiddenField.val("0");
});

var printButton = $("#PrintButton");

printButton.click(function () {
    var templatePath = "/Reports/Print.html";
    var headerPath = "/Reports/Assets/Header.aspx";
    var title = $("h2").html();
    var targetControlId = "PLAccountGridView";
    var date = now;
    var windowName = "PLAccountGridView";
    var offsetFirst = 0;
    var offsetLast = 0;

    printGridView(templatePath, headerPath, title, targetControlId, date, user, office, windowName, offsetFirst, offsetLast);
});
