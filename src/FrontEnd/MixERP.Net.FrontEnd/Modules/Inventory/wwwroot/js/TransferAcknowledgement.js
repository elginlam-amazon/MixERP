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
    Moved VERBATIM out of the inline <script> block of TransferAcknowledgement.ascx by the
    .ascx -> .razor porting pass - a .razor file may not contain a <script> element (Blazor
    strips it), so TransferAcknowledgement.razor appends this file from the shared base's
    OnAfterRenderAsync, BEFORE wwwroot/js/TransferRequest.js.

    THAT ORDER IS LOAD-BEARING, and it is the order the .ascx had: the two module-scope
    override variables below are read by TransferRequest.js when it builds each row's
    check-list and print links. Loading this file second would leave both undefined and send
    every row link to the transfer-REQUEST screens instead of the delivery ones.

    #ReceiveButton is the receive controller: it calls getSelectedItems() (defined in
    TransferRequest.js), takes the FIRST ticked id out of #SelectedValuesHidden, confirms,
    posts, and navigates to the resulting TRANSFER's check list with the id the service
    returned.

    URL CHANGE, the ONLY change made to this code:
      /Modules/Inventory/Services/TransferAcknowledgement.asmx/Receive
          -> /services/inventory/transferacknowledgement/receive
    The response handling is untouched: `msg.d` still carries the new transaction id because
    the ASMX {"d": ...} envelope is preserved by Endpoints/AsmxCompatibility.cs, and
    logAjaxErrorMessage still reads the raw refusal message out of the preserved
    {Message, StackTrace, ExceptionType} + HTTP 500 error payload.
*/

var receiveButton = $("#ReceiveButton");

var checkListUrlOverride = "Confirmation/TransferDelivery.mix?TranId=%s";
var reportUrlOverride = "Reports/InventoryTransferDeliveryReport.mix?TranId=%1$s";

receiveButton.click(function() {
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

    var ajaxReceive = Receive(tranId);

    ajaxReceive.success(function (msg) {
        var id = msg.d;
        var transferUrl = "/Modules/Inventory/Confirmation/Transfer.mix?TranId=" + id;

        window.location = transferUrl;
    });

    ajaxReceive.fail(function(xhr) {
        logAjaxErrorMessage(xhr);
    });
});

function Receive(tranId) {

    //WAS: "/Modules/Inventory/Services/TransferAcknowledgement.asmx/Receive"
    url = "/services/inventory/transferacknowledgement/receive";

    data = appendParameter("", "tranId", tranId);

    data = getData(data);

    return getAjax(url, data);
};
