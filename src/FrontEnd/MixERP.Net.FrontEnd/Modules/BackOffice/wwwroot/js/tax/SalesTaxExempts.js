/*
    Moved VERBATIM out of the inline <script> block of Tax/SalesTaxExempts.ascx.

    WHY IT IS A FILE NOW: a .razor file may not contain a <script> element (Blazor strips it),
    so the ported Tax/SalesTaxExempts.razor loads this from OnAfterRenderAsync(firstRender)
    through the host's /js/mixerp/page-interop.js loadScripts helper.

    THE GLOBAL NAME IS THE CONTRACT: ScrudFactory's Scrud.js calls scrudCustomValidator() BY
    BARE NAME before a save, so it must stay a global function with this exact name. It blocks
    the save when the valid-till date precedes the valid-from date, or when the "to" price is
    not greater than the "from" price - dropping it would silently let invalid sales-tax
    exemptions be saved.
*/

function scrudCustomValidator() {
    var validFromTextbox = $("#valid_from_textbox");
    var validTillTextbox = $("#valid_till_textbox");
    var priceFromTextbox = $("#price_from_textbox");
    var priceToTextbox = $("#price_to_textbox");

    var validFrom = parseDate(validFromTextbox.val());
    var validTill = parseDate(validTillTextbox.val());
    var priceFrom = parseFloat2(priceFromTextbox.val());
    var priceTo = parseFloat2(priceToTextbox.val());

    if (validTill < validFrom) {
        displayMessage(Resources.Warnings.DateErrorMessage());
        return false;
    };
    if (priceTo <= priceFrom) {
        displayMessage(Resources.Warnings.ComparePriceErrorMessage());
        return false;
    };

    return true;
};
