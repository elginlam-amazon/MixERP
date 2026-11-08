/*
    Trial-balance screen controller for Modules/Finance/Reports/TrialBalance.

    EXTRACTED VERBATIM from the inline <script type="text/javascript"> block of
    Modules/Finance/Reports/TrialBalance.ascx. It had to leave the markup: Blazor STRIPS
    <script> elements inside a .razor file, so the ported component loads this file instead, on
    the render AFTER the asynchronous query - the document-ready block below reads the rendered
    #TrialBalanceGridView rows.

    THE THREE LOCALIZED GLOBALS (perviousPeriodLocalized - the misspelling is the original's -
    currentPeriodLocalized, closingLocalized) are registered by the component through the same
    "TrialBalance_Vars" key PageUtility.RegisterJavascript used, and AWAITED, so they exist
    before this file runs. The English fallbacks below are the original's and are kept.

    POSITIONAL GRID CONTRACT: the grouped header is built by MOVING header cells 1 and 2 into a
    prepended row with rowspan=2, and the footer totals sum columns 2..7 - so the eight columns
    must stay in their original order (account_number, account, previous debit/credit,
    debit/credit, closing debit/credit).

    The three change handlers below are the ONLY writers of #IsCompactHidden,
    #IncludeZeroBalanceAccountHidden and #ChangeSideWhenNegativeHidden, which the ported
    server-side Show handler reads back through the module's interop reader.
*/

if (typeof perviousPeriodLocalized === "undefined") {
    perviousPeriodLocalized = "Previous Period";
};

if (typeof currentPeriodLocalized === "undefined") {
    currentPeriodLocalized = "Current Period";
};

if (typeof closingLocalized === "undefined") {
    closingLocalized = "Closing";
};

var grid = $("#TrialBalanceGridView");
var fromDateTextBox = $("#FromDateTextBox");
var toDateTextBox = $("#ToDateTextBox");
var compactCheckBox = $("#CompactCheckBox");
var zeroBalanceCheckBox = $("#ZeroBalanceCheckBox");
var changeSideCheckBox = $("#ChangeSideCheckBox");
var factorInputText = $("#FactorInputText");
var compact = $("#CompactCheckBox");
var changeSideCheckBox = $("#ChangeSideCheckBox");
var zeroBalanceCheckBox = $("#ZeroBalanceCheckBox");

var isCompactHiddenField = $("#IsCompactHidden");
var includeZeroBalanceAccountHidden = $("#IncludeZeroBalanceAccountHidden");
var changeSideWhenNegativeHidden = $("#ChangeSideWhenNegativeHidden");
var printButton = $("#PrintButton");

$(document).ready(function () {
    var html = "<tr><th></th><th></th><th colspan='2'>" + perviousPeriodLocalized + "</th><th colspan='2'>" + currentPeriodLocalized + "</th><th colspan='2'>" + closingLocalized + "</th></tr>";
    var thead = grid.find("thead");
    thead.prepend(html);

    var accountNumberCell = thead.find("tr:nth-child(2)").find("th:first-child");
    var accountCell = thead.find("tr:nth-child(2)").find("th:nth-child(2)");

    thead.find("tr:first-child").find("th:first-child").html(accountNumberCell.html()).attr("rowspan", "2");
    thead.find("tr:first-child").find("th:nth-child(2)").html(accountCell.html()).attr("rowspan", "2");

    accountNumberCell.remove();
    accountCell.remove();

    var previousDebit = sumOfColumn(grid, 2);
    var previousCredit = sumOfColumn(grid, 3);

    var debit = sumOfColumn(grid, 4);
    var credit = sumOfColumn(grid, 5);

    var closingDebit = sumOfColumn(grid, 6);
    var closingCredit = sumOfColumn(grid, 7);

    var tfoot = "<tr class='active strong text-right'><td colspan='2'>Total</td><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>";
    tfoot = String.format(tfoot, previousDebit, previousCredit, debit, credit, closingDebit, closingCredit);

    grid.append(tfoot);

});

compactCheckBox.change(function () {
    if (compactCheckBox.is(":checked")) {
        isCompactHiddenField.val("1");
        return;
    };

    isCompactHiddenField.val("0");
});

zeroBalanceCheckBox.change(function () {
    if (zeroBalanceCheckBox.is(":checked")) {
        includeZeroBalanceAccountHidden.val("1");
        return;
    };

    includeZeroBalanceAccountHidden.val("0");
});

changeSideCheckBox.change(function () {
    if (changeSideCheckBox.is(":checked")) {
        changeSideWhenNegativeHidden.val("1");
        return;
    };

    changeSideWhenNegativeHidden.val("0");
});

printButton.click(function () {
    var report = "TrialBalanceReport.mix?FromDate={0}&ToDate={1}&Factor={2}&Compact={3}&ChangeSide={4}&IncludeZeroBalanceAccounts={5}";
    var fromDate = Date.parseExact(fromDateTextBox.val(), window.shortDateFormat).toDateString();
    var toDate = Date.parseExact(toDateTextBox.val(), window.shortDateFormat).toDateString();
    var factor = factorInputText.val();
    var compact = compactCheckBox.is(":checked");
    var changeSide = changeSideCheckBox.is(":checked");
    var includeZeroBalanceAccounts = zeroBalanceCheckBox.is("checked");

    report = String.format(report, fromDate, toDate,factor,compact,changeSide,includeZeroBalanceAccounts);
    showWindow(report);
});
