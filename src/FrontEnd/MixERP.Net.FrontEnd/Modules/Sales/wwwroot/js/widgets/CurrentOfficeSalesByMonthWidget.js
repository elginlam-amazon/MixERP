// Moved VERBATIM from the inline <script> block of
// Widgets/CurrentOfficeSalesByMonthWidget.ascx. Blazor strips <script> inside a .razor
// file, so the ported component loads this file once its hidden data table has rendered.
// prepareChart() is a global of the host's wwwroot/js/mixerp/mixerp-core.js.
$(document).ready(function () {
    prepareChart("SalesByMonthGridView", "curr-office-sales-by-month-canvas", "curr-office-sales-by-month-legend", 'line', false);
});
