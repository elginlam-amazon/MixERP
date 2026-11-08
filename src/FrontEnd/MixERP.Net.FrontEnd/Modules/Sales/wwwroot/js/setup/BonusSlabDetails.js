// Moved VERBATIM from the inline <script> block of Setup/BonusSlabDetails.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Setup/BonusSlabDetails.razor) loads this file after its first render.
// Scrud.js calls scrudCustomValidator() before a save, by that exact global name.
function scrudCustomValidator() {
    var amountFromTextBox = $("#amount_from_textbox");
    var amountToTextBox = $("#amount_to_textbox");

    var from = parseFloat2(amountFromTextBox.val());
    var to = parseFloat2(amountToTextBox.val());

    if (to <= from) {
        displayMessage(Resources.Warnings.CompareAmountErrorMessage());
        return false;
    };

    return true;
};
