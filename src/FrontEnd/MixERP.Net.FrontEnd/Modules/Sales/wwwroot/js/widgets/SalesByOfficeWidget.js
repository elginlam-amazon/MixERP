// Moved VERBATIM from the inline <script> block of Widgets/SalesByOfficeWidget.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Widgets/SalesByOfficeWidget.razor) loads this file once its hidden data table has
// rendered. prepareChart() is a global of the host's wwwroot/js/mixerp/mixerp-core.js.
$(document).ready(function () {
    prepareChart("SalesByOfficeGridView", "sales-by-month-canvas", "sales-by-month-legend", 'bar');
});
