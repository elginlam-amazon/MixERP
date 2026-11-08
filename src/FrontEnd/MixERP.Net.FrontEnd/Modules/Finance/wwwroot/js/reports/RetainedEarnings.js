/*
    Print handler for Modules/Finance/Reports/RetainedEarnings.

    EXTRACTED VERBATIM from the inline <script type="text/javascript"> block of
    Modules/Finance/Reports/RetainedEarnings.ascx. It had to leave the markup: Blazor STRIPS
    <script> elements inside a .razor file, so the ported component loads this file instead, on
    the render AFTER the query - the three selectors below read elements out of the DOM.

    ⚠ Reports/RetainedEarningsPopup DELIBERATELY DOES NOT LOAD THIS FILE: that .ascx carried no
    script block even though it shared the same code-behind, so its #PrintButton has always
    been inert. Loading this there would newly switch a button on.
*/

var printButton = $("#PrintButton");
var dateTextBox = $("#DateTextBox");
var factorInputText = $("#FactorInputText");


printButton.click(function () {
    var report = "RetainedEarningsReport.mix?Date={0}&Factor={1}";
    var date = Date.parseExact(dateTextBox.val(), window.shortDateFormat).toDateString();
    var factor = factorInputText.val();

    report = String.format(report, date, factor);
    showWindow(report);
});
