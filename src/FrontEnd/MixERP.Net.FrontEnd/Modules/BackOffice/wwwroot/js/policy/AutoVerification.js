/*
    Moved VERBATIM out of the inline <script> block of Policy/AutoVerification.ascx.

    WHY IT IS A FILE NOW: a .razor file may not contain a <script> element (Blazor strips it),
    so the ported Policy/AutoVerification.razor loads this from
    OnAfterRenderAsync(firstRender) through the host's /js/mixerp/page-interop.js loadScripts
    helper.

    THE GLOBAL NAME IS THE CONTRACT: ScrudFactory's Scrud.js calls scrudCustomValidator() BY
    BARE NAME before a save, so it must stay a global function with this exact name. It blocks
    the save when the policy's end date precedes its effective-from date - dropping it would
    silently let a policy row be saved with an impossible date range.

    The two module-level $(...) lookups and the $(document).ready() call that invokes the
    validator once on load are the original's, and are kept exactly as written - including the
    fact that the ready() call IGNORES the return value (it is there for its displayMessage
    side effect on an already-invalid row, not to gate anything).
*/

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
