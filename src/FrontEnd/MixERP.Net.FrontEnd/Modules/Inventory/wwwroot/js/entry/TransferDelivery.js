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
    Moved VERBATIM out of the inline <script> block of Entry/TransferDelivery.ascx by the
    .ascx -> .razor porting pass - a .razor file may not contain a <script> element, so
    Entry/TransferDelivery.razor appends this file from OnAfterRenderAsync.

    TWO HOST OBLIGATIONS live in here:
      * StockAdjustmentFactory_FormvView_SaveButton_Callback - called by
        StockAdjustmentFactory's own FormView.js by that exact (misspelled) global name when
        #SaveButton is clicked. Renaming it silently breaks saving.
      * appendToTable(...) / the $(document).ready block - loadModel() pre-fills the transfer
        grid from the originating transfer REQUEST (?RequestId=), using FormView.js's
        appendToTable global and its #TransferGridView loading class.

    URL CHANGES, the ONLY changes made to this code:
      /Modules/Inventory/Services/Entry/TransferDelivery.asmx/GetModel
          -> /services/inventory/entry/transferdelivery/getmodel
      /Modules/Inventory/Services/Entry/TransferDelivery.asmx/Save
          -> /services/inventory/entry/transferdelivery/save
    The response handling is untouched because the ASMX {"d": ...} envelope - and the
    PascalCase member names this file reads off each model (StoreName, ItemCode, ItemName,
    UnitName, Quantity) - are preserved by Endpoints/AsmxCompatibility.cs, which serializes
    with Newtonsoft and no naming policy.
*/

var grid = $("#TransferGridView");

$(document).ready(function() {
    grid.addClass("loading");
    var requestId = parseFloat(getQueryStringByName("RequestId") || 0);

    if (!requestId) {
        return;
    };


    var ajaxGetModel = GetModel(requestId);

    ajaxGetModel.success(function (msg) {
        loadModel(msg.d);
    });

    ajaxGetModel.fail(function (xhr) {
        logAjaxErrorMessage(xhr);
    });

});

function loadModel(models) {

    for (i = 0; i < models.length; i++) {
        var storeName = models[i].StoreName;
        var itemCode = models[i].ItemCode;
        var itemName = models[i].ItemName;
        var unitName = models[i].UnitName;
        var quantity = models[i].Quantity;

        appendToTable("Cr", storeName, itemCode, itemName, unitName, quantity);
    };

    grid.removeClass("loading");
};

function GetModel(requestId) {
    //WAS: "/Modules/Inventory/Services/Entry/TransferDelivery.asmx/GetModel"
    url = "/services/inventory/entry/transferdelivery/getmodel";

    data = appendParameter("", "requestId", requestId);

    data = getData(data);
    return getAjax(url, data);
};


function StockAdjustmentFactory_FormvView_SaveButton_Callback() {
    var valueDate = Date.parseExact(valueDateTextBox.val(), window.shortDateFormat);
    var referenceNumber = referenceNumberInputText.val();
    var statementReference = statementReferenceTextArea.val();
    var requestId = parseFloat(getQueryStringByName("RequestId") || 0);
    var sourceStoreId = parseFloat($("#SourceStoreSelect").val() || 0);
    var shipperId = parseFloat($("#ShippingCompanySelect").val() || 0);


    if (!requestId) {
        displayMessage(Resources.Warnings.InvalidRequestId(), "error");
        return;
    };

    if (!sourceStoreId) {
        displayMessage(Resources.Warnings.InvalidStore(), "error");
        makeDirty($("#SourceStoreSelect"));
        return;
    };

    if (!shipperId) {
        displayMessage(Resources.Warnings.InvalidShippingCompany(), "error");
        makeDirty($("#ShippingCompanySelect"));
        return;
    };


    var tableData = tableToJSON(transferGridView);

    var ajaxSaveDelivery = SaveDelivery(valueDate, referenceNumber, statementReference, requestId, sourceStoreId, shipperId, tableData);

    ajaxSaveDelivery.success(function (msg) {
        var id = msg.d;
        window.location = "/Modules/Inventory/Confirmation/TransferDelivery.mix?TranId=" + id;
    });

    ajaxSaveDelivery.fail(function (xhr) {
        logAjaxErrorMessage(xhr);
    });
};

function SaveDelivery(valueDate, referenceNumber, statementReference, requestId, sourceStoreId, shipperId, tableData) {

    //WAS: "/Modules/Inventory/Services/Entry/TransferDelivery.asmx/Save"
    url = "/services/inventory/entry/transferdelivery/save";

    data = appendParameter("", "valueDate", valueDate);
    data = appendParameter(data, "referenceNumber", referenceNumber);
    data = appendParameter(data, "statementReference", statementReference);
    data = appendParameter(data, "requestId", requestId);
    data = appendParameter(data, "sourceStoreId", sourceStoreId);
    data = appendParameter(data, "shipperId", shipperId);
    data = appendParameter(data, "data", tableData);
    data = getData(data);
    return getAjax(url, data);
};
