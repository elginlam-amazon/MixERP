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
var scrudShowCompact = function () {
    var url = updateQueryString('show', 'compact');
    url = updateQueryString('Page', '1', url);
    window.location = url;
};

var scrudShowAll = function () {
    var url = updateQueryString('show', 'all');
    url = updateQueryString('Page', '1', url);
    window.location = url;
};

var scrudConfirmAction = function () {
    var retVal = false;
    var selectedItemValue;

    var confirmed = confirm(Resources.Questions.AreYouSure());

    if (confirmed) {
        selectedItemValue = scrudGetSelectedRadioValue();

        if (selectedItemValue == undefined) {
            alert(Resources.Titles.NothingSelected());
            retVal = false;
        } else {
            retVal = true;
            if (customFormUrl && keyColumn) {
                window.location = customFormUrl + "?" + keyColumn + "=" + selectedItemValue;
            }
        }
    }
    return retVal;
};

var scrudSelectAndClose = function () {
    var lastValueHidden = $("#LastValueHidden");
    lastValueHidden.val(scrudGetSelectedRadioValue());
    scrudSaveAndClose();
};

var scrudGetSelectedRadioValue = function () {
    return $('[id^="SelectRadio"]:checked').val();
};

var scrudSelectRadioById = function (id) {
    $('[id^="SelectRadio"]').prop("checked", false);

    var radio = $("#" + id);
    radio.prop("checked", true);

    //  jQuery .prop() does NOT fire a change event, so the radio's Blazor @onchange handler
    //  never runs and the component would keep an empty selection - which silently killed
    //  edit/delete and forced every save down the INSERT branch. In WebForms the radio was a
    //  posted form field, so no notification was needed. Push the selection explicitly.
    if (scrudComponentRef) {
        scrudComponentRef.invokeMethodAsync("SelectRow", radio.val());
    };
};

var scrudPrintGridView = function () {
    var user = $("#" + userIdHiddenId).val();
    var office = $("#" + officeCodeHiddenId).val();
    var title = $("#" + titleLabelId).html();

    printGridView(reportTemplatePath, reportHeaderPath, title, formGridViewId, date, user, office, 'ScrudReport', 1, 0);
};

//--------------------------------------------------------------------------------------------
// MIGRATION NOTE (checklist task 81). "Sys.WebForms.PageRequestManager" was the ASP.NET AJAX
// partial-postback manager, shipped by the ScriptManager control. That control has no modern
// equivalent and was dropped (checklist task 32), so the object no longer exists and an
// unguarded reference here would throw while this file is being evaluated - killing EVERY
// function below it. The hook is kept, guarded, so a page that still runs the legacy
// ScriptManager behaves exactly as before, and its modern replacement is initScrud(), which
// ScrudForm invokes from OnAfterRenderAsync after every Blazor re-render.
//--------------------------------------------------------------------------------------------
if (typeof Sys !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
        //Fired on each ASP.net AJAX request.
        scrudInitialize();
    });
};

$(document).ready(function () {
    scrudInitialize();
});

//--------------------------------------------------------------------------------------------
// The IDEMPOTENT re-initialization entry point (checklist task 81).
//
// Scrud.js wired its handlers from $(document).ready and re-wired them from the ASP.NET AJAX
// add_endRequest hook above. Neither fires after a Blazor re-render, so a grid the component
// re-rendered would come back with dead row-click handlers. ScrudForm calls this through
// JSRuntime.InvokeVoidAsync("initScrud") after every render; it is safe to call any number of
// times because scrudInitialize() now un-binds its namespaced handler before binding it.
//--------------------------------------------------------------------------------------------
//  The component reference ScrudForm publishes so a row click can reach the server.
//  Kept at module scope so a later initScrud() call without an argument (or a call made
//  before the component is ready) does not drop an already-good reference.
var scrudComponentRef = null;

window.initScrud = function (componentRef) {
    if (componentRef) {
        scrudComponentRef = componentRef;
    };

    if (typeof jQuery === "undefined") {
        return;
    };

    scrudInitialize();
};

var scrudInitialize = function () {
    //Registering grid row click event to automatically select the radio.
    //The ".scrud" event namespace plus the leading "off" is what makes repeated calls
    //idempotent: without it every re-render would stack another click handler on every row.
    $('#' + formGridViewId + ' tr').off('click.scrud').on('click.scrud', function () {
        //Grid row was clicked. Now, searching the radio button.
        var radio = $(this).find('td input:radio');

        //The radio button was found.
        scrudSelectRadioById(radio.attr("id"));
    });

    scrudSaveAndClose();
    scrudLayout();
    scrudOnServerError();
};

function scrudOnServerError() {
    var errorField = $("#ScrudError");

    //  #ScrudError is now rendered UNCONDITIONALLY (it is this script's write target on an
    //  ajax failure), so its mere existence no longer means "there was an error". The legacy
    //  control only assigned the id at the moment it had something to say, which is what this
    //  test reproduced. Testing CONTENT restores that meaning - otherwise displayForm() ran on
    //  every single render and permanently hid the browse grid.
    if (errorField.length && $.trim(errorField.text()).length) {
        displayForm();
    };
};

function scrudLayout() {
    var gridPanel = $("#GridPanel");
    var scrudUpdatePanel = $("#ScrudUpdatePanel");

    if (gridPanel && scrudUpdatePanel) {
        gridPanel.css("width", scrudUpdatePanel.width() + "px");
    };
};

function scrudGetQueryStringByName(name) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

function getParent() {
    var parent;

    if (window.opener && window.opener.document) {
        parent = window.opener;
    }

    if (parent == undefined) {
        parent = window.parent;
    }

    return parent;
};

function scrudSaveAndClose() {
    var parent = getParent();

    if (parent) {
        var lastValue = parseFloat2($("#LastValueHidden").val());
        var ctl = scrudGetQueryStringByName('AssociatedControlId');
        var associatedControl = parent.$('#' + ctl);
        var callBackFunctionName = scrudGetQueryStringByName('CallBackFunctionName');

        if (lastValue > 0) {
            associatedControl.val(lastValue);

            if (callBackFunctionName) {
                parent[callBackFunctionName]();
            };

            closeWindow();
        }
    }
}

function closeWindow() {
    if (window.opener && window.opener.document) {
        top.close();
    } else {
        parent.jQuery.colorbox.close();
    };
};

function scrudDispalyLoading() {
    $("#FormPanel").find("div.segment").addClass("loading");
};
function scrudRemoveLoading() {
    $("#FormPanel").find("div.segment").removeClass("loading");
};

function scrudUpdateTableHeaders() {
    $("div.floating-header").each(function () {
        var originalHeaderRow = $(".tableFloatingHeaderOriginal", this);
        var floatingHeaderRow = $(".tableFloatingHeader", this);
        var offset = $(this).offset();
        var scrollTop = $(window).scrollTop();
        if ((scrollTop > offset.top) && (scrollTop < offset.top + $(this).height())) {
            floatingHeaderRow.css("visibility", "visible");
            floatingHeaderRow.css("z-index", "1");
            floatingHeaderRow.css("top", Math.min(scrollTop - offset.top, $(this).height() - floatingHeaderRow.height()) + "px");

            // Copy cell widths from original header
            $("th", floatingHeaderRow).each(function (index) {
                var cellWidth = $("th", originalHeaderRow).eq(index).css('width');
                $(this).css('width', cellWidth);
            });

            // Copy row width from whole table
            floatingHeaderRow.css("width", $("table.grid").css("width"));
        } else {
            floatingHeaderRow.css("visibility", "hidden");
            floatingHeaderRow.css("top", "0");
        }
    });
};

$(document).ready(function () {
    $("table.grid").each(function () {
        $(this).wrap("<div class=\"floating-header\" style=\"position:relative\"></div>");

        var originalHeaderRow = $("tr:first", this);
        originalHeaderRow.before(originalHeaderRow.clone());
        var clonedHeaderRow = $("tr:first", this);

        clonedHeaderRow.addClass("tableFloatingHeader");
        clonedHeaderRow.css("position", "absolute");
        clonedHeaderRow.css("top", "0");
        clonedHeaderRow.css("left", $(this).css("margin-left"));
        clonedHeaderRow.css("visibility", "hidden");

        originalHeaderRow.addClass("tableFloatingHeaderOriginal");
    });
    scrudUpdateTableHeaders();
    $(window).scroll(scrudUpdateTableHeaders);
    $(window).resize(scrudUpdateTableHeaders);
});

var scrudAddNew = function () {
    if (customFormUrl) {
        top.location = customFormUrl;
    }

    $('#' + formGridViewId + 'tr').find('td input:radio').prop('checked', false);
    $('#form1').each(function () {
        this.reset();
    });

    displayForm();

    scrudRepaint();

    if (typeof scrudAddNewCallBack === "function") {
        scrudAddNewCallBack();
    };
    //Prevent postback
    return false;
};

function displayForm() {
    $('#' + gridPanelId).hide(500);
    $('#' + formPanelId).show(500);

    if (typeof scrudFormDisplayedCallBack === "function") {
        scrudFormDisplayedCallBack();
    };
};

var scrudRepaint = function () {
    setTimeout(function () {
        $(document).trigger('resize');
    }, 1000);
};

$(document).ready(function () {
    shortcut.add("ESC", function () {
        if ($('#' + formPanelId).is(':hidden')) {
            return;
        };

        if ($("#colorbox").css("display") === "block") {
            return;
        };

        if ($("body").attr("class") === "modal-open") {
            return;
        };

        var result = confirm(Resources.Questions.AreYouSure());
        if (result) {
            $('#' + cancelButtonId).click();
        }
    });

    shortcut.add("RETURN", function () {
        scrudSelectAndClose();
    });

    shortcut.add("CTRL+SHIFT+C", function () {
        scrudShowCompact();
    });

    shortcut.add("CTRL+SHIFT+S", function () {
        scrudShowAll();
    });

    shortcut.add("CTRL+SHIFT+A", function () {
        return (scrudAddNew());
    });

    shortcut.add("CTRL+SHIFT+E", function () {
        $('#EditButtontop').click();
    });

    shortcut.add("CTRL+SHIFT+D", function () {
        $('#DeleteButtontop').click();
    });

    shortcut.add("CTRL+SHIFT+P", function () {
        scrudPrintGridView();
    });
});

function scrudClientValidation() {
    //MIGRATION NOTE. "Page_ClientValidate" was emitted by the WebForms validation pipeline,
    //which no longer exists: the authoritative gate is now ScrudForm.Validate()/IsValid, which
    //enforces every rule SERVER-side before anything is persisted. The function is kept because
    //consumers' own scripts call it (and it still raises the loading affordance and still honours
    //a page-supplied scrudCustomValidator), but the missing global is guarded instead of
    //throwing - with no client validator present it simply proceeds, exactly as the server gate
    //expects.
    var clientValid = typeof Page_ClientValidate !== "function" || Page_ClientValidate("");

    if (clientValid) {
        scrudDispalyLoading();

        if (typeof scrudCustomValidator === "function") {
            var isValid = scrudCustomValidator();

            if (!isValid) {
                scrudRemoveLoading();
            };

            return isValid;
        };

        return true;
    };
};