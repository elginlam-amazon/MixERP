/*
    Statement-of-cash-flows screen controller for Modules/Finance/Reports/CashFlow.

    EXTRACTED VERBATIM from the inline <script type="text/javascript"> block of
    Modules/Finance/Reports/CashFlow.ascx. It had to leave the markup: Blazor STRIPS <script>
    elements inside a .razor file, so the ported component loads this file instead, on the
    render AFTER the asynchronous query - everything below reads the rendered
    #CashFlowStatementGridView out of the DOM.

    POSITIONAL GRID CONTRACT (why the ported component renders the auto-generated columns in
    the data source's own order, and keeps the trailing boolean as a disabled CHECKBOX):
      td:last / th:last  - the "is summation" flag; the row is marked `warning` + `strong` when
                           its input is :checked, and the column is then REMOVED
      td:not(:eq(0))     - every value cell; reddened when negative
      td:first-child     - indented for non-summation rows
*/

var grid = $("#CashFlowStatementGridView");

$(document).ready(function () {
    grid.find("tr").each(function () {
        var isSummation = $(this).find("td:last, th:last");

        if (isSummation.find("input").is(":checked") === true) {
            $(this).addClass("warning");
            $(this).find("td").addClass("strong");
        };

        isSummation.remove();

        $(this).find("td").not(":eq(0)").each(function () {
            var value = parseFloat2($(this).html());

            if (value < 0) {
                $(this).addClass("error");
            };
        });

    });

    grid.find("tr").each(function () {
        if ($(this).is(".positive, .negative") === false) {
            var cell = $(this).find("td:first-child");
            cell.html("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cell.html());
        };
    });

    grid.find("tr:nth-child(1), tr:last-child").each(function () {
        $(this).addClass("positive");
    });

});

var printButton = $("#PrintButton");

printButton.click(function () {
    var templatePath = "/Reports/Print.html";
    var headerPath = "/Reports/Assets/Header.aspx";
    var title = $("h2").html();
    var targetControlId = "CashFlowStatementGridView";
    var date = now;
    var windowName = "CashFlowStatementGridView";
    var offsetFirst = 0;
    var offsetLast = 0;

    printGridView(templatePath, headerPath, title, targetControlId, date, user, office, windowName, offsetFirst, offsetLast);
});
