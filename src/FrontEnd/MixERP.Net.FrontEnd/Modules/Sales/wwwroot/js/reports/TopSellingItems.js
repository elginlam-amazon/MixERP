// Moved VERBATIM from the inline <script> block of Reports/TopSellingItems.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Reports/TopSellingItems.razor) loads this file after its first render.
// getDocHeight() is a global of the host's wwwroot/js/mixerp/mixerp-core.js.
$(document).ready(function () {
    var iframe = $("iframe");

    iframe.css("width", "100%");
    iframe.css("height", getDocHeight() + "px");
    iframe.css("border", "0");
})
