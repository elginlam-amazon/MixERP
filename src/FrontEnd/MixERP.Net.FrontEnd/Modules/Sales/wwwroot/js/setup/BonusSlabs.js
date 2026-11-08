// Moved VERBATIM from the inline <script> block of Setup/BonusSlabs.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Setup/BonusSlabs.razor) loads this file after its first render.
// Scrud.js calls scrudCustomValidator() before a save, by that exact global name.
// NOTE: the two jQuery lookups below run at PARSE time, exactly as they did in the
// original inline block (the elements do not exist yet, so both are empty sets until
// the Scrud form re-renders them) - the behaviour is preserved as-is, not "fixed".

var effectiveFromTextbox = $("#effective_from_textbox");
var endsOnTextbox = $("#ends_on_textbox");

$(document).ready(function () {
    scrudCustomValidator();
});

function scrudCustomValidator() {
    var effectiveFrom = parseDate(effectiveFromTextbox.val());
    var endsOn = parseDate(endsOnTextbox.val());

    if (endsOn < effectiveFrom) {
        displayMessage(Resources.Warnings.DateErrorMessage());
        return false;
    };
    return true;
};
