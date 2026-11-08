// Moved VERBATIM from the inline <script> block of Setup/RecurringInvoiceSetup.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Setup/RecurringInvoiceSetup.razor) loads this file after its first render.
// Scrud.js calls scrudCustomValidator() before a save, by that exact global name.
function scrudCustomValidator() {
    var startFromTextbox = $("#starts_from_textbox");
    var endsOnTextbox = $("#ends_on_textbox");

    var startForm = parseDate(startFromTextbox.val());
    var endsOn = parseDate(endsOnTextbox.val());

    if (endsOn < startForm) {
        displayMessage(Resources.Warnings.DateErrorMessage());
        return false;
    };
    return true;
};
