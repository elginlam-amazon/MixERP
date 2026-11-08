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

/*jshint -W098*/
/*global createFlaggedRows, getSelectedCheckBoxItemIds, Resources, popUnder, shortcut, toogleSelection*/

/*
    BLAZOR RE-WIRING NOTE (see migration-summary.md section 3.6a).

    Under WebForms every request was a full page load: this script ran with the whole grid
    already in the DOM, and the top-level $('#ProductViewGridView tr').click(...) binding
    plus the $(document).ready(updateFlagColor) call therefore always found the data rows.

    Under Blazor none of that holds. JSUtility.AddJSReference injects the script element
    once per circuit (keyed), it can be appended BEFORE the component markup reaches the
    browser, and - critically - the grid's <tr> set is REPLACED by Blazor on every
    re-render (the Show button, a flag update, any LoadGridView()). A binding made once at
    load time attaches to rows that either do not exist yet or are later thrown away, so
    row-click selection and the flag row colouring render but do nothing.

    The row-dependent wiring therefore lives in window.initStockTransactionView(), which
    StockTransactionView.razor calls from OnAfterRenderAsync on EVERY render, not just the
    first. Element lookups are re-done on each call (never cached across calls) and the
    binding uses .off().on() with a namespaced event, so repeat calls REBIND rather than
    stack duplicate handlers - the function is idempotent. The handlers' behaviour is
    unchanged from the original.

    The static-markup wiring (#PrintButton, the ALT+O / CTRL+ENTER shortcuts) stays at
    load time: those elements are present from the component's first render and are never
    replaced.
*/

var printButton = $("#PrintButton");

printButton.click(function () {
    var templatePath = "/Reports/Print.html";
    var headerPath = "/Reports/Assets/Header.aspx";
    var title = $("h2").html();
    var targetControlId = "ProductViewGridView";
    var date = now;
    var windowName = "ProductView";
    var offsetFirst = 2;
    var offsetLast = 2;

    printGridView(templatePath, headerPath, title, targetControlId, date, user, office, windowName, offsetFirst, offsetLast);
});

var updateFlagColor = function () {
    //Get an instance of the form grid.
    var grid = $("#ProductViewGridView");
    createFlaggedRows(grid);
};

//Idempotent re-wiring of everything that depends on the grid's data rows. Called by
//StockTransactionView.razor from OnAfterRenderAsync after every render, and once at load
//time below (so the control behaves exactly as it did under WebForms when the markup is
//already present when the script arrives).
window.initStockTransactionView = function () {
    var grid = $("#ProductViewGridView");

    //The component is not in the DOM yet (the script was injected before the markup
    //reached the browser). The next render's call will wire it up.
    if (!grid.length) {
        return;
    }

    //Port of the original top-level $('#ProductViewGridView tr').click(...) binding.
    //Namespaced .off().on() so a repeat call rebinds the new rows instead of stacking
    //handlers on them.
    grid.find("tr").off("click.stocktransactionview").on("click.stocktransactionview", function () {
        var checkBox = $(this).find('td input:checkbox');
        toogleSelection(checkBox);
    });

    //Port of the original $(document).ready(updateFlagColor). This reads the trailing
    //FlagBackgroundColor / FlagForegroundColor columns off the rendered rows, so it has to
    //re-run whenever the rows change or the flag colours never appear.
    updateFlagColor();
};

var getSelectedItems = function () {
    //Get the grid instance.
    var grid = $("#ProductViewGridView");

    //Set the position of the column which contains the checkbox.
    var checkBoxColumnPosition = "2";

    //Set the position of the column which contains id.
    var idColumnPosition = "3";

    var selection = getSelectedCheckBoxItemIds(checkBoxColumnPosition, idColumnPosition, grid);

    if (selection.length > 0) {
        $("#SelectedValuesHidden").val(selection.join(','));
        return true;
    } else {
        $.notify(Resources.Titles.NothingSelected(), "error");
        return false;
    }
};

//Blazor interop reader. getSelectedItems() publishes the selection with
//$("#SelectedValuesHidden").val(...), and jQuery's .val() raises NO input/change event,
//so a Blazor @bind on that element would never fire and the server would never see the
//selection. The server therefore reads the element's value directly through this function
//after invoking getSelectedItems(). Keep the element id in sync with the markup.
var readSelectedValuesHidden = function () {
    return $("#SelectedValuesHidden").val();
};

$(document).ready(function () {
    //Self-initialisation, matching the original's $(document).ready(updateFlagColor) and
    //top-level row binding. Repeat renders are wired by StockTransactionView.razor calling
    //initStockTransactionView(); the namespaced .off().on() above makes both paths safe
    //together.
    window.initStockTransactionView();

    shortcut.add("ALT+O", function () {
        $('#OfficeTextBox').foucs();
    });

    shortcut.add("CTRL+ENTER", function () {
        $('#ShowButton').click();
    });
});

function DropDown(el) {
    this.dd = el;
    this.placeholder = this.dd.children('span');
    this.opts = this.dd.find('ul.dropdown > li');
    this.val = '';
    this.index = -1;
    this.initEvents();
}

DropDown.prototype = {
    initEvents: function () {
        var obj = this;

        obj.dd.on('click', function (event) {
            $(this).toggleClass('active');
            event.stopPropagation();
        });

        obj.opts.on('click', function () {
            var opt = $(this);
            obj.val = opt.text();
            obj.index = opt.index();
            obj.placeholder.text(obj.val);
        });
    },

    getValue: function () {
        return this.val;
    },
    getIndex: function () {
        return this.index;
    }
};

$(function () {
    $(document).click(function () {
        // all dropdowns
        $('.wrapper-dropdown-5').removeClass('active');
    });
});