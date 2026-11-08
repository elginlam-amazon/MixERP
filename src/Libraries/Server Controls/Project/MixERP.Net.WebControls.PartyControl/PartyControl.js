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
    MIGRATION NOTE (net45 WebForms -> net10.0 Blazor Server)

    Behaviour of every handler below is UNCHANGED. The only structural change is that the
    element lookups, the event wiring and the initial loadParties() call now live inside
    window.initPartyControl() instead of running once at script-evaluation time.

    Why: under WebForms a full page load re-executed this file against a fresh DOM every
    time the control was rendered. Under Blazor Server the <script> tag is injected only
    ONCE per key ("PartyControl"), so top-level selector caching would leave goButton /
    partyDropDownList / partyCodeTextBox / every *Span bound to detached nodes the second
    time the component is instantiated (SPA navigation away and back) - the control would
    render and silently do nothing. PartyControl.razor therefore calls initPartyControl()
    from OnAfterRenderAsync(firstRender) for EVERY component instance.

    initPartyControl() is idempotent: it re-queries the elements and uses .off().on() so a
    repeat call rebinds rather than stacking duplicate handlers.
*/

var url = "";
var data = "";

//Variables
var baseCurrency = "";
var officeDue = 0;
var partyCode = "";
var totalDue = 0;

window.initPartyControl = function () {
    var partyDropDownList = $("#PartyDropDownList");

    //The component is not in the DOM yet (the script was injected before the markup
    //reached the browser) - the self-initialisation at the bottom of this file, or the
    //next component instance, will wire it up.
    if (!partyDropDownList.length) {
        return;
    }

    //Idempotence guard, keyed on the ELEMENT rather than on a global flag: a freshly
    //rendered component instance brings a new #PartyDropDownList node that carries no
    //flag and therefore does get wired, while a repeat call against the SAME node is a
    //no-op (so neither the handlers nor the loadParties() data bind are duplicated).
    if (partyDropDownList.data("partyControlInitialized")) {
        return;
    }

    partyDropDownList.data("partyControlInitialized", true);

    var addressDiv = $("#AddressDiv");

    var creditAllowedSpan = $("#CreditAllowedSpan");
    var cSTNumberSpan = $("#CSTNumberSpan");

    var defaultCurrencySpan = $("#DefaultCurrencySpan");

    var emailAddressSpan = $("#EmailAddressSpan");

    var gLHeadSpan = $("#GLHeadSpan");
    var goButton = $("#GoButton");

    var lastPaymentDateSpan = $("#LastPaymentDateSpan");

    var maxCreditAmountSpan = $("#MaxCreditAmountSpan");
    var maxCreditPeriodSpan = $("#MaxCreditPeriodSpan");

    var officeDueAmountSpan = $("#OfficeDueAmountSpan");
    var officeDueAmountHidden = $("#OfficeDueAmountHidden");

    var pANNumberSpan = $("#PANNumberSpan");
    var partyCodeTextBox = $("#PartyCodeTextBox");
    var partyTypeSpan = $("#PartyTypeSpan");

    var shippingAddressesDiv = $("#ShippingAddressesDiv");
    var sSTNumberSpan = $("#SSTNumberSpan");

    var totalDueAmountSpan = $("#TotalDueAmountSpan");
    var totalDueAmountHidden = $("#TotalDueAmountHidden");
    var transactionValueSpan = $("#TransactionValueSpan");

    //Page Events
    loadParties();

    //Control Events
    goButton.off("click.partyControl").on("click.partyControl", function () {
        totalDueAmountSpan.html("");
        officeDueAmountSpan.html("");
        lastPaymentDateSpan.html("");
        transactionValueSpan.html("");

        partyCode = partyDropDownList.getSelectedValue();

        if (isNullOrWhiteSpace(partyCode)) {
            $.notify(Resources.Titles.NothingSelected(), "error");
            return;
        };

        var ajaxGetPartyDue = getPartyDueModel(partyCode);

        ajaxGetPartyDue.success(function (msg) {
            var model = msg.d;
            totalDue = convertToDebit(parseFloat2(model.TotalDueAmount));
            officeDue = convertToDebit(parseFloat2(model.OfficeDueAmount));

            baseCurrency = model.CurrencyCode;

            defaultCurrencySpan.html(baseCurrency);

            totalDueAmountSpan.html(getFormattedCurrency(model.CurrencySymbol, totalDue));
            totalDueAmountHidden.val(totalDue);

            officeDueAmountSpan.html(getFormattedCurrency(model.CurrencySymbol, officeDue));
            officeDueAmountHidden.val(officeDue);

            lastPaymentDateSpan.html(model.LastPaymentDate);
            transactionValueSpan.html(getFormattedCurrency(model.CurrencySymbol, model.TransactionValue));

            if (typeof goButtonCallBack == "function") {
                goButtonCallBack();
            };
        });

        ajaxGetPartyDue.fail(function (xhr) {
            logAjaxErrorMessage(xhr);
        });

        var ajaxGetPartyView = getPartyView(partyCode);
        var partyCurrencySymbol = "रू.";//Todo

        ajaxGetPartyView.success(function (msg) {
            var partyView = msg.d;
            partyTypeSpan.html(partyView.PartyType);
            emailAddressSpan.html(String.format("<a href='mailto:{0}'>{0}</a>", partyView.Email));
            pANNumberSpan.html(partyView.PANNumber);
            sSTNumberSpan.html(partyView.SSTNumber);
            cSTNumberSpan.html(partyView.CSTNumber);
            creditAllowedSpan.html(localizeBool(partyView.AllowCredit));
            maxCreditPeriodSpan.html(partyView.MaximumCreditPeriod + "&nbsp;" + Resources.Labels.DaysLowerCase());
            maxCreditAmountSpan.html(getFormattedCurrency(partyCurrencySymbol, partyView.MaximumCreditAmount));

            gLHeadSpan.html(partyView.GLHead);
            addressDiv.html(getAddress(partyView.ZipCode, partyView.AddressLine1, partyView.AddressLine2, partyView.Street, partyView.City, partyView.State, partyView.Country, partyView.Phone, partyView.Cell, partyView.Fax, partyView.Url));
        });

        ajaxGetPartyView.fail(function (xhr) {
            logAjaxErrorMessage(xhr);
        });

        var ajaxGetShippingAddresses = getShippingAddresses(partyCode);

        ajaxGetShippingAddresses.success(function (msg) {
            var shippingAddresses = [""];

            for (i = 0; i < msg.d.length; i++) {
                var address = msg.d[i];
                shippingAddresses.push(getAddress(address.POBox, address.AddressLine1, address.AddressLine2, address.Street, address.City, address.State, address.Country));
            };

            shippingAddressesDiv.html(shippingAddresses.join(""));
        });

        ajaxGetShippingAddresses.fail(function (xhr) {
            logAjaxErrorMessage(xhr);
        });
    });

    partyDropDownList.off("change.partyControl").on("change.partyControl", function () {
        partyCodeTextBox.val(partyDropDownList.getSelectedValue());
    });

    partyCodeTextBox.off("blur.partyControl").on("blur.partyControl", function () {
        partyDropDownList.val(partyCodeTextBox.val());
        if (partyDropDownList.val() != partyCodeTextBox.val()) {
            partyCodeTextBox.val("");
        };
    });
};

function getAddress(poBox, line1, line2, street, city, state, country, phone, cell, fax, url) {
    var html = [""];
    html.push("<address>");

    if (poBox) {
        html.push(poBox, "<br />");
    };

    if (line1) {
        html.push(line1, "<br />");
    };

    if (line2) {
        html.push(line2, "<br />");
    };

    html.push(street, "&nbsp;", city, "<br />");
    html.push(state, "&nbsp;", country, "<br />");

    if (phone) {
        html.push("✆ ", phone);

        if (cell) {
            html.push(", ", cell);
        };

        html.push("<br />");
    } else {
        if (cell) {
            html.push("✆ ", cell, "<br />");
        };
    };

    if (fax) {
        html.push("📠 ", fax, "<br />");
    };

    if (url) {
        html.push("⊕ <a target='_blank' href='", url, "'>", url, "</a>");
    };

    html.push("</address>");

    return html.join("");
};

function getFormattedCurrency(symbol, value) {
    var money;

    if (value < 0) {
        money = symbol + " " + $.number(value * -1, currencyDecimalPlaces, decimalSeparator, thousandSeparator);
        money = "<div class='error-message'><strong>(" + money + ")</strong></div>";
    } else {
        money = symbol + " " + $.number(value, currencyDecimalPlaces, decimalSeparator, thousandSeparator);
    }

    return money;
};

function localizeBool(val) {
    if (val) {
        return Resources.Titles.Yes();
    };

    return Resources.Titles.No();
};

function getPartyView(partyCode) {
    //Todo
    url = "/Modules/Inventory/Services/PartyData.asmx/GetPartyView";//Todo--Parametrize these.
    data = appendParameter("", "partyCode", partyCode);
    data = getData(data);

    return getAjax(url, data);
};

function getPartyDueModel(partyCode) {
    //Todo
    url = "/Modules/Inventory/Services/PartyData.asmx/GetPartyDue";
    data = appendParameter("", "partyCode", partyCode);
    data = getData(data);

    return getAjax(url, data);
};

function getShippingAddresses(partyCode) {
    url = "/Modules/Inventory/Services/PartyData.asmx/GetShippingAddresses";
    data = appendParameter("", "partyCode", partyCode);
    data = getData(data);

    return getAjax(url, data);
};

//Ajax Data Binding
function loadParties() {
    url = "/Modules/Inventory/Services/PartyData.asmx/GetParties";
    ajaxDataBind(url, $("#PartyDropDownList"));
};

//Self-initialisation on first injection. This is the direct replacement for the former
//top-level execution plus `$(document).ready(function () { loadParties(); });`: when the
//<script> element is appended the component markup is normally already in the DOM, so the
//control is wired exactly as it was under WebForms without waiting for a round trip.
//Repeat instances are wired by PartyControl.razor calling initPartyControl() from
//OnAfterRenderAsync; the element-keyed guard above makes the two paths safe together.
if (window.jQuery) {
    $(function () {
        window.initPartyControl();
    });
}
