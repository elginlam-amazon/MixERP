// Moved VERBATIM from the inline <script> block of
// Widgets/TopSellingProductOfAllTimeCurrentWidget.ascx. Blazor strips <script> inside a
// .razor file, so the ported component loads this file once its hidden data table has
// rendered. preparePieChart() is a global of the host's wwwroot/js/mixerp/mixerp-core.js.
$(document).ready(function () {
    preparePieChart('TopSellingProductsOfAllTimeGridView', 'top-selling-products-canvas', 'top-selling-products-legend', 'pie', true);
});
