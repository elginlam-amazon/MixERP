/*jshint -W032*/

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
    Moved VERBATIM out of the inline <script> block of Setup/CUOM.ascx by the .ascx ->
    .razor porting pass: a .razor file may not contain a <script> element (Blazor strips
    it), so Setup/CUOM.razor appends this file from OnAfterRenderAsync instead.

    scrudCustomValidator() is a GLOBAL HOOK, not a local helper - ScrudFactory's Scrud.js
    calls it by bare global name before it saves, and a missing definition means the
    base/compare unit equality check silently stops running. The function name must not
    change.
*/

function scrudCustomValidator() {
    var baseUnitIdDropdownlist = $("#base_unit_id_dropdownlist");
    var compareUnitIdDropdownlist = $("#compare_unit_id_dropdownlist");

    var baseUnit = parseInt2(baseUnitIdDropdownlist.getSelectedValue());
    var compareUnit = parseInt2(compareUnitIdDropdownlist.getSelectedValue());

    if (baseUnit === compareUnit) {
        displayMessage(Resources.Errors.CompoundUnitOfMeasureErrorMessage());
        return false;
    };
    return true;
};
