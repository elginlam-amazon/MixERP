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
    WHY THIS FILE EXISTS - it is the replacement for WebForms POSTBACK VALUE RECOVERY, not
    a new feature.

    Two Inventory screens have a server-side click handler whose inputs are written
    CLIENT-SIDE by their own javascript, never typed by the user:

      * Adjustment        - ShowButton_Click read `storeHiddenField.Value`, and
                            ShowButton_ClientClick() in Adjustment.js is what fills
                            #StoreHidden from the #StoreSelect selection.
      * Reports/AccountStatement - ShowButton_Click / Flag_Updated read #StoreIdHidden
                            (filled by storeSelect.blur in reports/AccountStatement.js),
                            #ItemCodeInputText (filled by createCascadingPair from
                            #ItemSelect) and #SelectedValuesHidden (filled by
                            getSelectedItems()).

    Under WebForms those values arrived on the postback because they were form fields.
    Blazor has no postback: a value written into an input by jQuery raises no input event,
    so the component never sees it. Reading the element at click time is therefore the
    FAITHFUL equivalent of the postback read - it is the same value, at the same moment,
    from the same element. Nothing is invented and no new element id is introduced; every
    id these functions are called with is one the legacy DOM already had.

    `eval` is deliberately NOT used - this is an ES module with two explicit, narrow
    functions, imported by the component through IJSRuntime.
*/

export function getValue(elementId) {
    if (!elementId) {
        return null;
    }

    const element = document.getElementById(elementId);

    if (!element) {
        return null;
    }

    return element.value === undefined || element.value === null ? null : element.value;
}

export function setValue(elementId, value) {
    if (!elementId) {
        return;
    }

    const element = document.getElementById(elementId);

    if (element) {
        element.value = value === null || value === undefined ? "" : value;
    }
}
