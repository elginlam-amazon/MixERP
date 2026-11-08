/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

/*
    Moved VERBATIM out of the inline <script> block of TransferAuthorization.ascx by the
    .ascx -> .razor porting pass - a .razor file may not contain a <script> element (Blazor
    strips it), so TransferAuthorization.razor appends this file from the shared base's
    OnAfterRenderAsync, AFTER wwwroot/js/TransferRequest.js, which is the document order the
    .ascx had.

    It is the authorize/reject controller: both handlers call getSelectedItems() (defined in
    TransferRequest.js), take the FIRST ticked id out of #SelectedValuesHidden, confirm, post,
    and reload the page on success.

    URL CHANGES, the ONLY changes made to this code:
      /Modules/Inventory/Services/TransferAuthorization.asmx/Authorize
          -> /services/inventory/transferauthorization/authorize
      /Modules/Inventory/Services/TransferAuthorization.asmx/Reject
          -> /services/inventory/transferauthorization/reject
    The response handling is untouched: the ASMX {"d": ...} success envelope and the
    {Message, StackTrace, ExceptionType} + HTTP 500 error payload that logAjaxErrorMessage
    reads are both preserved by Endpoints/AsmxCompatibility.cs - which matters here, because
    the admin-only / invalid-id refusals of those two methods ARE that message.
*/

var authorizeButton = $("#AuthorizeButton");
var rejectButton = $("#RejectButton");

authorizeButton.click(function () {
    getSelectedItems();
    var tranId = parseFloat($("#SelectedValuesHidden").val().split(",")[0] || 0);

    if (!tranId) {
        displayMessage(Resources.Warnings.NothingSelected(), "error");
        return;
    };

    var confirmed = confirm(Resources.Questions.AreYouSure());

    if (!confirmed) {
        return;
    };

    var ajaxAuthorize = Authorize(tranId);

    ajaxAuthorize.success(function () {
        window.location = window.location;
    });

    ajaxAuthorize.fail(function (xhr) {
        logAjaxErrorMessage(xhr);
    });

});

function Authorize(tranId) {

    //WAS: "/Modules/Inventory/Services/TransferAuthorization.asmx/Authorize"
    url = "/services/inventory/transferauthorization/authorize";

    data = appendParameter("", "tranId", tranId);

    data = getData(data);

    return getAjax(url, data);
};

rejectButton.click(function () {
    getSelectedItems();
    var tranId = parseFloat($("#SelectedValuesHidden").val().split(",")[0] || 0);

    if (!tranId) {
        displayMessage(Resources.Warnings.NothingSelected(), "error");
        return;
    };

    var confirmed = confirm(Resources.Questions.AreYouSure());

    if (!confirmed) {
        return;
    };

    var ajaxReject = Reject(tranId);

    ajaxReject.success(function () {
        window.location = window.location;
    });

    ajaxReject.fail(function (xhr) {
        logAjaxErrorMessage(xhr);
    });

});

function Reject(tranId) {

    //WAS: "/Modules/Inventory/Services/TransferAuthorization.asmx/Reject"
    url = "/services/inventory/transferauthorization/reject";

    data = appendParameter("", "tranId", tranId);

    data = getData(data);

    return getAjax(url, data);
};
