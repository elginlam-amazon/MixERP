/*
    Moved out of the inline <script> block of Policy/ApiAccess.ascx.

    WHY IT IS A FILE NOW: a .razor file may not contain a <script> element (Blazor strips it),
    so the ported Policy/ApiAccess.razor loads this from OnAfterRenderAsync(firstRender) through
    the host's /js/mixerp/page-interop.js loadScripts helper. The `window.pocos` array it reads
    is emitted FIRST, through the same "ApiAccess_Vars" PageUtility.RegisterJavascript key the
    legacy code-behind used - and awaited, so it always exists before this file runs.

    WHAT THIS DOES, UNCHANGED: the Scrud form renders the policy's poco_type_name column as a
    free-text box, which is useless for picking a CLR type name. addPocoSelect() hides that box,
    prepends a <select> populated from window.pocos (every IPoco implementation the server
    found), and copies the chosen text back into the hidden box on blur so the Scrud still saves
    the text value it expects. The element ids #poco_type_name_textbox and #poco_type_name_select
    are ScrudFactory's own naming convention and are unchanged.

    ⚠ ONE BLOCK WAS DROPPED, DELIBERATELY:

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            addPocoSelect();
        });

    `Sys.WebForms.PageRequestManager` is the WebForms ScriptManager/UpdatePanel partial-rendering
    client, which DOES NOT EXIST on ASP.NET Core and is not loaded by any page here - the call
    would throw a ReferenceError on load, before the rest of this file ran. Its PURPOSE is
    preserved, not dropped: it re-ran addPocoSelect() after every partial postback re-rendered
    the form. The ported component achieves the same thing by invoking addPocoSelect() from
    OnAfterRenderAsync on EVERY render, not just the first - and the two guards below make that
    call idempotent (it returns immediately when the form is not on screen, and when the select
    is already attached), so it re-attaches the select exactly when the Scrud has replaced the
    form and does nothing otherwise. The select therefore survives a re-render as it did before.
*/

function addPocoSelect() {
    var pocoTypeNameTextbox = $("#poco_type_name_textbox");

    if (!pocoTypeNameTextbox.length) {
        //The Scrud form is not on screen (list view), so there is nothing to decorate.
        return;
    }

    if ($("#poco_type_name_select").length) {
        //Already decorated this instance of the form.
        return;
    }

    pocoTypeNameTextbox.hide();

    var select = "<select id='poco_type_name_select'><select>";

    pocoTypeNameTextbox.parent().prepend(select);

    var pocoTypeNameSelect = $("#poco_type_name_select");

    var options = window.pocos.toString().split(',');

    for (i = 0; i < options.length; i++) {
        pocoTypeNameSelect.append("<option>" + options[i] + "</option>");
    };


    pocoTypeNameSelect.blur(function () {
        pocoTypeNameTextbox.val($(this).getSelectedText());
    });
};

$(document).ready(function () {
    addPocoSelect();
});
