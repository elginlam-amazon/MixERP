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

using MixERP.Net.Common;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.ScrudFactory.Helpers;
using System;

namespace MixERP.Net.WebControls.ScrudFactory.Controls.TextBoxes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the date field of the schema-driven scrud form.
    ///     <para>
    ///         The factory used to new up a plain <c>TextBox</c> and then call
    ///         <c>jQueryUI.AddjQueryUIDatePicker(this.Page, id, …)</c> to attach the picker. That
    ///         whole job already belongs to the migrated
    ///         <c>MixERP.Net.WebControls.Common.DateTextBox</c> component, which owns the picker
    ///         interop in <c>OnAfterRenderAsync(firstRender)</c> and renders the same
    ///         <c>id</c>/<c>name</c>/<c>class="date"</c> input. So this renderer produces a
    ///         <see cref="ScrudFieldKind.DateTextBox" /> field and <c>ScrudFieldRow.razor</c>
    ///         DELEGATES to that component — no second datepicker is implemented here.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudDateTextBox
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Ported as-is: it is pure <c>Conversion</c>/<c>DateTime</c> logic with no legacy API.
        ///     A value with no time component renders short ("d"), otherwise full ("f").
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string GetLocalizedDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            DateTime date = Conversion.TryCastDate(value);

            if (date.Date == date)
            {
                return date.ToString("d");
            }

            return date.ToString("f");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddDateTextBox(HtmlTable, …)</c>: the <c>{column}_textbox</c> id (a date
        ///     column used the TEXTBOX suffix, not a suffix of its own — <c>GetFormCollection</c>
        ///     reads it back by that string, so it is preserved), the 100-character cap, the
        ///     localized default value, the read-only treatment when disabled, the <c>date</c> css
        ///     class, the date rule, and the required rule plus
        ///     <c>Titles.RequiredFieldIndicator</c>-suffixed label for a non-nullable column.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddDateTextBox(ScrudFieldTable fieldTable, string resourceClassName, string columnName, string defaultValue, bool isNullable, string validatorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            string id = columnName + "_textbox";

            ScrudField field = ScrudTextBox.GetTextBox(id, 100);

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.Kind = ScrudFieldKind.DateTextBox;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = validatorCssClass;

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            field.Value = GetLocalizedDate(defaultValue);
            field.ReadOnly = disabled;
            field.CssClass = "date";

            //GetDateValidator: the rule is NOT dropped - it is handed to the form's server-side
            //gate through ScrudField.Rules. The delegated DateTextBox component additionally
            //enforces its own date rules client- and server-side.
            field.AddRule(GetDateValidator(field, validatorCssClass));

            if (!isNullable)
            {
                field.Label = label + Titles.RequiredFieldIndicator;
                field.AddRule(ScrudFactoryHelper.GetRequiredFieldValidator(field, validatorCssClass));
                fieldTable.Add(field);
                return;
            }

            field.Label = label;
            fieldTable.Add(field);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetDateValidator</c>. The <c>System.Web.UI.WebControls.CompareValidator</c>
        ///     it returned (<c>Type = ValidationDataType.Date</c>,
        ///     <c>Operator = ValidationCompareOperator.DataTypeCheck</c>,
        ///     <c>ErrorMessage = "&lt;br/&gt;" + Titles.InvalidDate</c>,
        ///     <c>Display = Dynamic</c>) is a removed type, so the RULE is re-expressed as a
        ///     <see cref="ScrudValidationRule" /> which <see cref="ScrudField.Validate" /> enforces
        ///     in C# on the server. The validator's id suffix and its error message — including the
        ///     leading <c>&lt;br/&gt;</c> — are carried verbatim.
        ///     <para>
        ///         <c>EnableClientScript</c> and <c>SetFocusOnError</c> have no modern counterpart:
        ///         there is no WebForms client-validation pipeline to emit script into, and stealing
        ///         focus on every keystroke while the user is still typing would be a regression,
        ///         not a port (same resolution as the Common precedent §2). The message itself is
        ///         still shown, so the user is not left without feedback.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudValidationRule GetDateValidator(ScrudField fieldToValidate, string cssClass)
        {
            if (fieldToValidate == null)
            {
                return null;
            }

            ScrudValidationRule validator = new ScrudValidationRule();
            validator.Id = fieldToValidate.ElementId + "CompareValidator";
            validator.ErrorMessage = @"<br/>" + Titles.InvalidDate;
            validator.CssClass = cssClass;
            validator.DataType = ScrudValidationDataType.Date;
            validator.Operator = ScrudCompareOperator.DataTypeCheck;

            return validator;
        }
    }
}
