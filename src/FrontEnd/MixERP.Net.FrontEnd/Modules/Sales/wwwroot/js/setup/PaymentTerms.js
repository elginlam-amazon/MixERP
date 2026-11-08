// Moved VERBATIM from the inline <script> block of Setup/PaymentTerms.ascx.
// Blazor strips <script> inside a .razor file, so the ported component
// (Setup/PaymentTerms.razor) loads this file after its first render.
// Scrud.js calls scrudCustomValidator() before a save, by that exact global name.
function scrudCustomValidator() {
    var dueDaysTextbox = $("#due_days_textbox");
    var dueFrequencyIdDropdownlist = $("#due_frequency_id_dropdownlist");
    var lateFeeIdDropdownlist = $("#late_fee_id_dropdownlist");
    var lateFeePostingFrequencyIdDropdownlist = $("#late_fee_posting_frequency_id_dropdownlist");

    var dueDays = parseInt2(dueDaysTextbox.val());
    var dueFrequency = parseInt2(dueFrequencyIdDropdownlist.getSelectedValue());
    var lateFee = parseInt2(lateFeeIdDropdownlist.getSelectedValue());
    var lateFeePostingFrequency = parseInt2(lateFeePostingFrequencyIdDropdownlist.getSelectedValue());

    if (!dueFrequency && dueDays === 0 || dueFrequency && dueDays > 0) {
        displayMessage(Resources.Warnings.DueFrequencyErrorMessage());
        return false;
    };

    if (!lateFee && lateFeePostingFrequency || lateFee && !lateFeePostingFrequency) {
        displayMessage(Resources.Warnings.LateFeeErrorMessage());
        return false;
    };
    return true;
};
