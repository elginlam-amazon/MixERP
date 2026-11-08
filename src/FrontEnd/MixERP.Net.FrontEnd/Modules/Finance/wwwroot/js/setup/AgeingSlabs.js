/*
    scrudCustomValidator() for Modules/Finance/Setup/AgeingSlabs.

    EXTRACTED VERBATIM from the inline <script type="text/javascript"> block of
    Modules/Finance/Setup/AgeingSlabs.ascx. It had to leave the markup: Blazor STRIPS <script>
    elements inside a .razor file, so the ported component loads this file instead (from
    OnAfterRenderAsync).

    THE FUNCTION NAME IS THE CONTRACT - it must stay a bare global called
    `scrudCustomValidator`, because ScrudFactory's Scrud.js invokes it by that name before a
    save and treats a `false` return as "do not save". It is this screen's only validation:
    without it an ageing slab whose To-days is not greater than its From-days would be saved.
*/
function scrudCustomValidator() {
    var fromDaysTextbox = $("#from_days_textbox");
    var toDaysTextbox = $("#to_days_textbox");

    var fromDays = parseInt2(fromDaysTextbox.val());
    var toDays = parseInt2(toDaysTextbox.val());

    if (toDays <= fromDays) {
        displayMessage(Resources.Warnings.CompareDaysErrorMessage());
        return false;
    };
    return true;
};
