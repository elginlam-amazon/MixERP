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

/*jshint -W032, -W098, -W020 */
/*global addDanger, addLoader, ajaxDataBind, ajaxUpdateVal, appendParameter, getColumnText, getData, getFormattedNumber, isNullOrWhiteSpace, itemIdQuerySericeUrl, itemPopupUrl, itemServiceUrl, makeDirty, parseFloat2, parseInt2, removeDirty, removeLoader, Resources, setColumnText, shippingCompanyServiceUrl, shortcut, showWindow, storeServiceUrl, StockAdjustmentFactory_FormvView_SaveButton_Callback, tableToJSON, unitServiceUrl, validateSides, showWindow */

//---------------------------------------------------------------------------------------
// MIGRATION NOTE (net10.0 / Blazor Server) - read before editing.
//
// JSUtility/PageUtility inject this file's <script> only ONCE per key, whereas WebForms
// re-executed it on every page load. This file used to cache all 17 jQuery handles at top
// level and wire its events + run its initial data load at LOAD time. Under Blazor Server
// none of those nodes exist when the file executes (the component renders over the SignalR
// circuit, after the document is already "ready"), so:
//   * the 17 handles would all be empty jQuery sets,
//   * the $(document).ready trio would never fire, because ready has already fired, and
//   * Add / Save would be bound to nothing: green build, form renders, form dead.
//
// The handle lookups, the event wirings, the shortcuts and the initial data load therefore
// live in the idempotent window.initStockAdjustmentForm() below. The guard flag is kept on a
// DOM NODE (#TransferGridView) rather than in a module variable, so a freshly rendered form
// re-initialises while a re-render of the SAME form does not.
//
// Every selector string, handler body, validation sequence, positional column index and
// service-url global below is UNCHANGED from the legacy file.
//---------------------------------------------------------------------------------------

//Controls. Formerly initialised inline at module scope; now assigned by
//bindStockAdjustmentFormControls() against the live DOM.
var addButton;
var dateTextBox;
var errorLabel;
var itemIdHidden;
var itemCodeInputText;
var itemSelect;
var quantityInputText;
var referenceNumberInputText;
var saveButton;
var statementReferenceTextArea;
var storeSelect;
var transactionTypeSelect;
var transferGridView;
var unitSelect;
var valueDateTextBox;
var shippingCompanySelect;
var sourceStoreSelect;

var url = "";
var data = "";

function bindStockAdjustmentFormControls() {
    addButton = $("#AddButton");
    //DEAD SELECTOR, preserved: the server control never emitted an element with id
    //"DateTextBox" (the date input is #ValueDateTextBox). It resolved to an empty set in the
    //legacy app too, and nothing reads this handle. See migration-summary.md §2.
    dateTextBox = $("#DateTextBox");
    errorLabel = $("#ErrorLabel");
    //HOST-SUPPLIED id, NOT dead - do not remove this handle. No element with id "ItemIdHidden"
    //is emitted by the control, but the CONSUMER PAGE emits it: Transfer.ascx and
    //TransferRequest.ascx each render <input type="hidden" id="ItemIdHidden" />, and all three
    //consumers pass ItemPopupUrl with "&AssociatedControlId=ItemIdHidden" so the item popup
    //writes it. ajaxDataBindCallBack() below then reads it to complete the item lookup, so the
    //round trip is LIVE on those two pages (TransferDelivery.ascx omits the input, which simply
    //leaves the lookup inert there). Keep both this handle and ajaxDataBindCallBack's guard.
    //See migration-summary.md §3.7.
    itemIdHidden = $("#ItemIdHidden");
    itemCodeInputText = $("#ItemCodeInputText");
    itemSelect = $("#ItemSelect");
    quantityInputText = $("#QuantityInputText");
    referenceNumberInputText = $("#ReferenceNumberInputText");
    saveButton = $("#SaveButton");
    statementReferenceTextArea = $("#StatementReferenceTextArea");
    storeSelect = $("#StoreSelect");
    //CONDITIONAL id - absent when HideSides is set. The .length guards below are what make
    //absence the documented signal; it must never be rendered as an empty element instead.
    transactionTypeSelect = $("#TransactionTypeSelect");
    transferGridView = $("#TransferGridView");
    unitSelect = $("#UnitSelect");
    valueDateTextBox = $("#ValueDateTextBox");
    //CONDITIONAL id - absent unless DisplayShipper.
    shippingCompanySelect = $("#ShippingCompanySelect");
    //CONDITIONAL id - absent unless DisplaySourceStore.
    sourceStoreSelect = $("#SourceStoreSelect");
}

function wireStockAdjustmentFormEvents() {
    addButton.click(function () {
        addRow();
    });

    saveButton.click(function () {
        errorLabel.html("");

        if (transferGridView.find("tr").length === 2) {
            errorLabel.html(Resources.Warnings.GridViewEmpty());
            return false;
        };

        var tableData = tableToJSON(transferGridView);

        //Ignore resharper warning on this
        if (validateSides == true) {
            if (Validate(tableData)) {
                Callback();
                return true;
            };
            return false;
        };

        Callback();
        return true;
    });

    itemSelect.change(function () {
        itemCodeInputText.val(itemSelect.getSelectedValue());
        loadUnits();
    });

    itemSelect.blur(function () {
        itemCodeInputText.val(itemSelect.getSelectedValue());
        loadUnits();
    });

    itemCodeInputText.blur(function () {
        if (!isNullOrWhiteSpace(itemCodeInputText.val())) {
            itemSelect.val(itemCodeInputText.val());
        };
    });
}

function addStockAdjustmentFormShortcuts() {
    shortcut.add("F4", function () {
        url = itemPopupUrl;
        showWindow(url);
    });

    shortcut.add("CTRL+ENTER", function () {
        addButton.trigger('click');
    });
}

var addRow = function () {
    itemCodeInputText.val(itemSelect.getSelectedValue());

    var tranType = transactionTypeSelect.getSelectedValue();
    var storeName = storeSelect.getSelectedText();
    var itemCode = itemCodeInputText.val();
    var itemName = itemSelect.getSelectedText();
    var unitName = unitSelect.getSelectedText();
    var quantity = parseInt(quantityInputText.val() || 0);

    if (transactionTypeSelect.length) {
        if (isNullOrWhiteSpace(tranType) || tranType === Resources.Titles.Select()) {
            makeDirty(transactionTypeSelect);
            return;
        };
    };

    if (isNullOrWhiteSpace(storeName) || storeName === Resources.Titles.Select()) {
        makeDirty(storeSelect);
        return;
    };

    if (isNullOrWhiteSpace(itemCode)) {
        makeDirty(itemCodeInputText);
        return;
    };

    if (isNullOrWhiteSpace(itemName) || itemName === Resources.Titles.Select()) {
        makeDirty(itemSelect);
        return;
    };

    if (isNullOrWhiteSpace(unitName) || unitName === Resources.Titles.Select()) {
        makeDirty(unitSelect);
        return;
    };

    if (quantity <= 0) {
        makeDirty(quantityInputText);
        return;
    };

    removeDirty(transactionTypeSelect);
    removeDirty(storeSelect);
    removeDirty(itemCodeInputText);
    removeDirty(itemSelect);
    removeDirty(unitSelect);
    removeDirty(quantityInputText);

    appendToTable(tranType, storeName, itemCode, itemName, unitName, quantity);
    itemCodeInputText.val("");
    quantityInputText.val("");

    if (transactionTypeSelect.length) {
        transactionTypeSelect.focus();
        return;
    };

    storeSelect.attr("disabled", "disabled");
    itemCodeInputText.focus();
};

function appendToTable(tranType, storeName, itemCode, itemName, unitName, quantity) {
    var rows = transferGridView.find("tbody tr:not(:last-child)");
    var match = false;
    var html;

    if (transactionTypeSelect.length) {
        rows.each(function () {
            var row = $(this);
            if (getColumnText(row, 0) !== tranType &&
                getColumnText(row, 1) === storeName &&
                getColumnText(row, 2) === itemCode) {
                $.notify(Resources.Warnings.DuplicateEntry());

                makeDirty(itemSelect);
                match = true;
            };

            if (getColumnText(row, 0) === tranType &&
                getColumnText(row, 1) === storeName &&
                getColumnText(row, 2) === itemCode &&
                getColumnText(row, 4) === unitName) {
                setColumnText(row, 5, getFormattedNumber(parseFloat2(getColumnText(row, 5)) + quantity));

                addDanger(row);
                match = true;
                return;
            }
        });

        if (!match) {
            html = "<tr class='grid2-row'><td>" + tranType + "</td><td>" + storeName + "</td><td>" + itemCode + "</td><td>" + itemName + "</td><td>" + unitName + "</td><td class='text-right'>" + getFormattedNumber(quantity) + "</td>"
                + "</td><td><a class='pointer' onclick='removeRow($(this));'><i class='ui delete icon'></i></a><a class='pointer' onclick='toggleDanger($(this));'><i class='ui pointer check mark icon'></a></i><a class='pointer' onclick='toggleSuccess($(this));'><i class='ui pointer thumbs up icon'></i></a></td></tr>";
            transferGridView.find("tr:last").before(html);
        };

        return;
    };

    rows.each(function () {
        var row = $(this);
        if (getColumnText(row, 0) === storeName &&
            getColumnText(row, 1) === itemCode) {
            $.notify(Resources.Warnings.DuplicateEntry());

            makeDirty(itemSelect);
            match = true;
        };

        if (getColumnText(row, 0) === storeName &&
            getColumnText(row, 1) === itemCode &&
            getColumnText(row, 3) === unitName) {
            setColumnText(row, 4, getFormattedNumber(parseFloat2(getColumnText(row, 4)) + quantity));

            addDanger(row);
            match = true;
            return;
        }
    });

    if (!match) {
        html = "<tr class='grid2-row'><td>" + storeName + "</td><td>" + itemCode + "</td><td>" + itemName + "</td><td>" + unitName + "</td><td class='text-right'>" + getFormattedNumber(quantity) + "</td>"
            + "</td><td><a class='pointer' onclick='removeRow($(this));'><i class='ui delete icon'></i></a><a class='pointer' onclick='toggleDanger($(this));'><i class='ui pointer check mark icon'></a></i><a class='pointer' onclick='toggleSuccess($(this));'><i class='ui pointer thumbs up icon'></i></a></td></tr>";
        transferGridView.find("tr:last").before(html);
    };
};

//The save callback contract with the HOST page. The name is MISSPELLED ("FormvView") and all
//three consumer pages define exactly that name - it is kept byte-identical. See
//migration-summary.md §5.
function Callback() {
    if (typeof StockAdjustmentFactory_FormvView_SaveButton_Callback === "function") {
        StockAdjustmentFactory_FormvView_SaveButton_Callback();
    };
};

//POSITIONAL CONTRACT: the tableToJSON payload is indexed [0]=transaction type ("Dr"/"Cr"),
//[2]=item name, [4]=unit name, [5]=quantity. Those indices are the grid's rendered column
//order; changing the markup's column order silently breaks this balance check.
function Validate(tableData) {
    var table = JSON.parse(tableData);

    var model = function (itemName, unitName, dr, cr) {
        this.item = itemName;
        this.unit = unitName;
        this.debit = dr;
        this.credit = cr;
    };

    var models = [];
    var debit;
    var credit;
    var i;

    for (i = 0; i < table.length; i++) {
        debit = parseInt2(GetDebit(table, i));
        credit = parseInt2(GetCredit(table, i));

        var item = new model(table[i][2], table[i][4], debit, credit);
        var index = ItemIndex(models, item);

        if (index === -1) {
            models.push(item);
        }
        else {
            models[index].debit += debit;
            models[index].credit += credit;
        };
    };

    for (i = 0; i < models.length; i++) {
        if (models[i]["debit"] !== models[i]["credit"]) {
            $.notify(Resources.Errors.ReferencingSidesNotEqual());
            return false;
        };
    };

    return true;
};

function ItemIndex(models, item) {
    for (j = 0; j < models.length; j++) {
        if ((models[j]["item"] === item["item"])) {
            return j;
        };
    };
    return -1;
};

function GetDebit(table, index) {
    return table[index][0] === "Dr" ? table[index][5] : 0;
};

function GetCredit(table, index) {
    return table[index][0] === "Cr" ? table[index][5] : 0;
};

//Ajax Data-binding
function loadStores() {
    if (storeSelect.length) {
        url = storeServiceUrl;
        ajaxDataBind(url, storeSelect);
    };

    if (sourceStoreSelect.length) {
        url = storeServiceUrl;
        ajaxDataBind(url, sourceStoreSelect);
    };
};

//Ajax Data-binding
function loadShippers() {
    if (shippingCompanySelect.length) {
        url = shippingCompanyServiceUrl;
        ajaxDataBind(url, shippingCompanySelect);
    };
};

function loadItems() {

    url = itemServiceUrl;
    data = appendParameter("", "tranBook", "StockAdjustment");
    data = getData(data);

    ajaxDataBind(url, itemSelect, data, null, itemCodeInputText, null, "ItemCode", "ItemName");
};

function loadUnits() {
    var itemCode = itemCodeInputText.val();

    url = unitServiceUrl;
    data = appendParameter("", "itemCode", itemCode);
    data = getData(data);

    addLoader(transferGridView);
    ajaxDataBind(url, unitSelect, data, null, null, removeLoaderInstance);
};

function removeLoaderInstance() {
    removeLoader(transferGridView);
};

//Check if ItemPopup window has updated the hidden field.
//Called by the shared ajaxDataBind helper, so it must stay a GLOBAL function declaration.
function ajaxDataBindCallBack(targetControl) {
    if (targetControl.is(itemSelect)) {
        var itemId = parseInt(itemIdHidden.val() || 0);

        itemIdHidden.val("");

        if (itemId > 0) {
            var targetControls = $([]);
            targetControls.push(itemCodeInputText);
            targetControls.push(itemSelect);

            url = itemIdQuerySericeUrl;
            data = appendParameter("", "itemId", itemId);
            data = getData(data);

            ajaxUpdateVal(url, data, targetControls);
        }
    };
};

//---------------------------------------------------------------------------------------
// The public init contract. StockAdjustmentForm.razor calls this from
// OnAfterRenderAsync(firstRender) for EVERY instance, once the markup the handles bind to is
// actually in the DOM.
//
// There is deliberately NO $(document).ready() self-initialisation: the file is appended to
// an already-ready document, so a ready handler would fire the instant the file executes -
// before (or without) the form being in the DOM at all.
//
// Idempotent: the flag lives on #TransferGridView, so calling it twice for the same rendered
// form is a no-op (no double-bound handlers, no duplicated shortcut registration), while a
// newly rendered form initialises normally.
//---------------------------------------------------------------------------------------

window.initStockAdjustmentForm = function () {
    var root = document.getElementById("TransferGridView");

    if (!root) {
        //The form is not in the DOM (yet). Nothing to initialize.
        return;
    };

    if (root.getAttribute("data-stock-adjustment-form-initialized") === "true") {
        //This very form instance has already been initialized.
        return;
    };

    root.setAttribute("data-stock-adjustment-form-initialized", "true");

    bindStockAdjustmentFormControls();
    wireStockAdjustmentFormEvents();
    addStockAdjustmentFormShortcuts();

    //Former $(document).ready body, unchanged. It would otherwise never run: ready has
    //already fired by the time a Blazor component renders.
    loadStores();
    loadItems();
    loadShippers();
};
