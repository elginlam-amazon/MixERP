// Moved VERBATIM from the inline <script> block of Setup/RecurringInvoices.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Setup/RecurringInvoices.razor) loads this file after its first render.
// Scrud.js calls scrudCustomValidator() before a save, by that exact global name.
function scrudCustomValidator() {
    var itemIdDropdownlist = $("#item_id_dropdownlist");
    var compoundItemIdDropdownlist = $("#compound_item_id_dropdownlist");
    var recurringAmountTextbox = $("#recurring_amount_textbox");

    var itemId = parseInt2(itemIdDropdownlist.getSelectedValue());
    var compoundItemId = parseInt2(compoundItemIdDropdownlist.getSelectedValue());
    var recurringAmount = parseFloat2(recurringAmountTextbox.val());

    if (!itemId  && ! compoundItemId || itemId && compoundItemId) {
        displayMessage(Resources.Warnings.ItemErrorMessage());
        return false;
    };
    if (recurringAmount <= 0) {
        displayMessage(Resources.Warnings.RecurringAmountErrorMessage());
        return false;
    };
    return true;
};
