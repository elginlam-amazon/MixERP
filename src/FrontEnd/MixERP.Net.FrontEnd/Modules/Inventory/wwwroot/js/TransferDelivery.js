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
    Moved VERBATIM out of the inline <script> block of TransferDelivery.ascx (the root-level
    LIST screen, not Entry/TransferDelivery) by the .ascx -> .razor porting pass - a .razor
    file may not contain a <script> element, so TransferDelivery.razor appends this file from
    the shared base's OnAfterRenderAsync, BEFORE wwwroot/js/TransferRequest.js.

    THAT ORDER IS LOAD-BEARING, and it is the order the .ascx had: the two module-scope
    override variables below are read by TransferRequest.js when it builds each row's
    check-list and print links. Loading this file second would leave both undefined and send
    every row link to the transfer-REQUEST screens instead of the delivery ones.

    NO URL CHANGED IN THIS FILE, because it calls no service: #SendButton reads the FIRST
    ticked id out of #SelectedValuesHidden (filled by getSelectedItems(), defined in
    TransferRequest.js) and NAVIGATES to the delivery entry screen with it. The entry screen
    is what posts.
*/

var sendButton = $("#SendButton");
var checkListUrlOverride = "Confirmation/TransferDelivery.mix?TranId=%s";
var reportUrlOverride = "Reports/InventoryTransferDeliveryReport.mix?TranId=%1$s";

sendButton.click(function () {
    getSelectedItems();
    var tranId = parseFloat($("#SelectedValuesHidden").val().split(",")[0] || 0);

    if (!tranId) {
        displayMessage(Resources.Warnings.NothingSelected(), "error");
        return;
    };


    var href = "Entry/TransferDelivery.mix?RequestId=" + tranId;
    window.location.href = href;

});
