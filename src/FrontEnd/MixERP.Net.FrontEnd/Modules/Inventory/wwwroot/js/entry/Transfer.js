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
    Moved VERBATIM out of the inline <script> block of Entry/Transfer.ascx by the .ascx ->
    .razor porting pass - a .razor file may not contain a <script> element (Blazor strips
    it), so Entry/Transfer.razor appends this file from OnAfterRenderAsync.

    StockAdjustmentFactory_FormvView_SaveButton_Callback IS A HOST OBLIGATION, not a local
    helper: StockAdjustmentFactory's own FormView.js calls it by that exact (misspelled)
    global name when the form's #SaveButton is clicked. Renaming it silently breaks saving.
    It reads valueDateTextBox / referenceNumberInputText / statementReferenceTextArea /
    transferGridView, which are FormView.js's own module-scope handles.

    URL CHANGE, and the ONLY change made to this code: the former
    /Modules/Inventory/Services/Entry/Transfer.asmx/Save is now the minimal-API endpoint
    /services/inventory/entry/transfer/save. The response handling is untouched because the
    ASMX {"d": ...} envelope is preserved by Endpoints/AsmxCompatibility.cs.
*/

function StockAdjustmentFactory_FormvView_SaveButton_Callback() {
    var valueDate = Date.parseExact(valueDateTextBox.val(), window.shortDateFormat);
    var referenceNumber = referenceNumberInputText.val();
    var statementReference = statementReferenceTextArea.val();
    var tableData = tableToJSON(transferGridView);

    var ajaxSaveTransfer = SaveTransfer(valueDate, referenceNumber, statementReference, tableData);

    ajaxSaveTransfer.success(function (msg) {
        var id = msg.d;
        window.location = "/Modules/Inventory/Confirmation/Transfer.mix?TranId=" + id;
    });

    ajaxSaveTransfer.fail(function (xhr) {
        logAjaxErrorMessage(xhr);
    });
};

function SaveTransfer(valueDate, referenceNumber, statementReference, tableData) {

    //WAS: "/Modules/Inventory/Services/Entry/Transfer.asmx/Save"
    url = "/services/inventory/entry/transfer/save";

    data = appendParameter("", "valueDate", valueDate);
    data = appendParameter(data, "referenceNumber", referenceNumber);
    data = appendParameter(data, "statementReference", statementReference);
    data = appendParameter(data, "data", tableData);
    data = getData(data);
    return getAjax(url, data);
};
