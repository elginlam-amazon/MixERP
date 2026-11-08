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
    Moved VERBATIM out of the inline <script> block of the deleted Reorder.ascx. A .razor
    file may not contain a <script> element (Blazor strips it), so Reorder.razor loads this
    file through Internal/ModuleScripts from OnAfterRenderAsync - which is also exactly when
    #ReorderGrid exists, the precondition this file's top-level block already had in
    WebForms (the inline block sat AFTER the GridView in the document).

    ONLY THE FOUR SERVICE URLS CHANGED. Every selector, every nth-child index, every
    callback name and every quirk is untouched - see the REVIEW notes below for the two
    upstream bugs deliberately carried across.
*/

var reorderGrid = $("#ReorderGrid");

if (reorderGrid) {

    reorderGrid.on("click", "td", function () {
        var checkBox = $(this).parent().find("input[type=checkbox]");

        console.log(checkBox.is(":checked"));

        if (checkBox.is(":checked")) {
            checkBox.removeProp("checked");
        } else {
            checkBox.prop("checked", "checked");
        }

        check(checkBox);
    });

    //REVIEW - PRESERVED UPSTREAM BUG: `e` is not a parameter of this handler, so this call
    //throws a ReferenceError and the click therefore does NOT stop propagating - meaning a
    //click on an input or select inside a row still toggles the row checkbox above. Fixing
    //it would change the grid's interaction behaviour, so it is carried verbatim.
    reorderGrid.on("click", "input, select", function () {
        e.stopPropagation();
    });

    reorderGrid.addClass("ui form nowrap table segment");

    reorderGrid.find("tr td:first-child").each(function () {
        $(this).css("width", "40px");
        $(this).html("<input type='checkbox' onclick='check($(this));' />");
    });

    reorderGrid.find("tr td:nth-child(6)").each(function () {
        $(this).css("width", "224px");
        $(this).css("padding", "0");
        $(this).html("<select data-role='party' disabled='disabled'></select>");
    });

    reorderGrid.find("tr td:nth-child(7)").each(function () {
        $(this).css("width", "124px");
        $(this).css("padding", "0");
        $(this).html("<select data-role='unit' disabled='disabled'></select>");
    });

    reorderGrid.find("tr td:nth-child(8)").each(function () {
        var value = $(this).parent().find("td:nth-child(13)").html();
        $(this).css("width", "78px");
        $(this).css("padding", "0");
        $(this).html(String.format("<input type='text' data-role='quantity' class='integer' disabled='disabled' value={0} />", value));
    });

    reorderGrid.find("tr td:nth-child(9)").each(function () {
        var value = $(this).parent().find("td:nth-child(15)").html();
        $(this).css("width", "100px");
        $(this).css("padding", "0");
        $(this).html(String.format("<input type='text' data-role='price' class='integer' disabled='disabled' value={0} />", value));
    });

    reorderGrid.find("tr td:nth-child(10)").each(function () {
        $(this).css("width", "120px");
        $(this).css("padding", "0");
        $(this).html("<select data-role='tax' disabled='disabled' />");
    });

    loadParties();
    loadTaxes();
    loadUnits();
};

function check(sender) {
    sender.parent().parent().find("select").attr("disabled", !sender.is(":checked"));
    sender.parent().parent().find("input[type=text]").attr("disabled", !sender.is(":checked"));
};

//Ajax Data Binding
function loadParties() {
    //WAS: /Modules/Inventory/Services/PartyData.asmx/GetParties
    //OWED TO THE INVENTORY SPECIALIST - see the REVIEW note at the bottom of this file.
    url = "/services/inventory/partydata/getparties";
    var parties = $("[data-role='party']");
    ajaxDataBind(url, parties);
};

function loadTaxes() {
    var taxSelect = $("[data-role='tax']");

    if (taxSelect.length) {
        //WAS: /Modules/BackOffice/Services/TaxData.asmx/GetSalesTaxes
        //OWED TO THE BACKOFFICE SPECIALIST - see the REVIEW note at the bottom of this file.
        url = "/services/backoffice/taxdata/getsalestaxes";
        data = appendParameter("", "tranBook", "Purchase");
        data = getData(data);

        ajaxDataBind(url, taxSelect, data);
    };
};

function loadUnits() {

    reorderGrid.find("tr").not(":first-child").each(function () {
        var row = $(this);

        var itemCode = row.find("td:nth-child(3)").html();
        var unitSelect = row.find("td:nth-child(7)").find("select");

        //WAS: /Modules/Inventory/Services/ItemData.asmx/GetUnits
        //OWED TO THE INVENTORY SPECIALIST - see the REVIEW note at the bottom of this file.
        url = "/services/inventory/itemdata/getunits";
        data = appendParameter("", "itemCode", itemCode);
        data = getData(data);

        ajaxDataBind(url, unitSelect, data);

    });

};

function ajaxDataBindCallBack(targetControl) {
    var selectedText;

    targetControl.each(function () {
        var element = $(this);
        
        if (element.attr("data-role") === "party") {
            element.each(function () {
                selectedText = $(this).parent().parent().find("td:nth-child(14)").html();

                $(this).find("option").filter(function () {
                    return this.value === selectedText;
                }).attr("selected", true);
            });
        };

        if (element.attr("data-role") === "unit") {
            selectedText = element.parent().parent().find("td:nth-child(5)").html();
            element.find("option").filter(function () {
                return this.text === selectedText;
            }).attr("selected", true);
        };

        //REVIEW - PRESERVED UPSTREAM BUG: the grid has only FIFTEEN columns, so
        //td:nth-child(16) matches nothing and selectedText is undefined - the tax select is
        //therefore never pre-selected to the item's existing tax. Carried verbatim because
        //picking the "intended" column (15 is Price, 14 is PreferredSupplier - neither is a
        //tax) would be a guess that silently changes which tax a reorder is posted with.
        if (element.attr("data-role") === "tax") {
            selectedText = element.parent().parent().find("td:nth-child(16)").html();

            element.find("option").filter(function () {
                return this.text === selectedText;
            }).attr("selected", true);
        };

    });
};

function ReorderInputButtonClick() {

    function Detail(itemId, supplierCode, unitId, orderQuantity, price, tax) {
        this.ItemId = itemId;
        this.SupplierCode = supplierCode;
        this.UnitId = unitId;
        this.OrderQuantity = orderQuantity;
        this.Price = price;
        this.Tax = tax;
    };

    if (reorderGrid) {
        var details = new Array();

        reorderGrid.find("tr").not(":first-child").each(function () {
            var selected = $(this).find(":first-child").find("input").is(":checked");

            if (selected) {
                var itemId = parseInt2($(this).find("td:nth-child(2)").html());
                var supplierCode = $(this).find("td:nth-child(6)").find("select").getSelectedValue();
                var unitId = parseInt2($(this).find("td:nth-child(7)").find("select").getSelectedValue());
                var orderQuantity = parseFloat2($(this).find("td:nth-child(8)").find("input").val());
                var price = parseFloat2($(this).find("td:nth-child(9)").find("input").val());
                var tax = $(this).find("td:nth-child(10)").find("input").val();

                var detail = new Detail(itemId, supplierCode, unitId, orderQuantity, price, tax);
                details.push(detail);
            };
        });

        if (details.length === 0) {
            $.notify(Resources.Titles.NothingSelected());
            return;
        };
        var confirmLocalized = "This action will create purchase orders automatically for you. Would you like to continue?";
        if (confirm(confirmLocalized)) {
            var ajaxsaveReorder = saveReorder(details);

            ajaxsaveReorder.success(function (msg) {
                if (msg.d) {
                    window.location = "/Modules/Purchase/Order.mix";
                };
            });

            ajaxsaveReorder.fail(function (xhr) {
                logAjaxErrorMessage(xhr);
            });

        };
    };
};

function saveReorder(details) {
    var d;
    d = appendParameter("", "details", details);
    d = getData(d);

    //WAS: /Modules/Purchase/Services/Entry/Reorder.asmx/Save
    //Now Endpoints/PurchaseEntryEndpoints.MapPurchaseReorderService. The {"d": ...}
    //envelope is preserved, so the `if (msg.d)` test above is unchanged.
    var url = "/services/purchase/entry/reorder/save";
    return getAjax(url, d);
};

/*
    REVIEW - THREE CROSS-MODULE URLS THIS FILE DOES NOT OWN.

    Three of the four services this page calls belong to OTHER modules, whose endpoints are
    not written yet:

      /Modules/Inventory/Services/PartyData.asmx/GetParties   -> /services/inventory/partydata/getparties
      /Modules/Inventory/Services/ItemData.asmx/GetUnits      -> /services/inventory/itemdata/getunits
      /Modules/BackOffice/Services/TaxData.asmx/GetSalesTaxes -> /services/backoffice/taxdata/getsalestaxes

    They are repointed HERE AND NOW rather than left as .asmx paths, because an .asmx path
    resolves to nothing at all on ASP.NET Core - leaving them would guarantee three dead
    selects. The targets follow the naming convention Sales established and the module
    migration summary records (lowercased module + service + method, under /services), so
    they are predictions, not inventions.

    THE INVENTORY AND BACKOFFICE SPECIALISTS MUST CONFIRM these three routes match the ones
    they actually map, and correct them here if they diverge. The response shapes are
    already compatible: all three are read through mixerp-core.js's ajaxDataBind, which the
    preserved {"d": ...} envelope keeps working unchanged.
*/
