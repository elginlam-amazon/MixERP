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

using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.ScrudFactory.Helpers;

namespace MixERP.Net.WebControls.ScrudFactory.Controls.TextBoxes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the text field of the schema-driven scrud form.
    ///     <para>
    ///         The factory used to new up a <c>System.Web.UI.WebControls.TextBox</c> with
    ///         <c>ClientIDMode.Static</c> and add it to an <c>HtmlTable</c> cell. Both of those
    ///         types are removed on net10.0, so it now produces a <see cref="ScrudField" /> that
    ///         <c>ScrudFieldRow.razor</c> renders as the same
    ///         <c>&lt;input id="{column}_textbox"&gt;</c>.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudTextBox
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddTextBox(HtmlTable, …)</c>, branch for branch: the PASSWORD column-name
        ///     detection and its "fake-password" placeholder, the <c>color</c> css class for a COLOR
        ///     column, the read-only treatment of a disabled non-password column, the default value,
        ///     and the <c>Titles.RequiredFieldIndicator</c>-suffixed label plus required rule for a
        ///     non-nullable column.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddTextBox(ScrudFieldTable fieldTable, string resourceClassName, string columnName, string dataType, string defaultValue, bool isNullable, int maxLength, string errorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            bool isPasswordField = columnName.ToUpperInvariant().Contains("PASSWORD");

            if (isPasswordField && disabled)
            {
                defaultValue = "fake-password";
            }

            ScrudField field = GetTextBox(columnName + "_textbox", maxLength);

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.Kind = ScrudFieldKind.TextBox;
            field.DataType = dataType;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = errorCssClass;

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            if (dataType != null && dataType.ToUpperInvariant().Equals("COLOR"))
            {
                field.CssClass = "color";
            }

            if (isPasswordField)
            {
                //The original did textBox.Attributes.Add("type", "password") on top of the
                //type="text" a TextBox already rendered, which produced a DUPLICATE type attribute
                //(and browsers honour the first, so the field was not actually masked - a latent
                //bug). The intent is unambiguous, so a single type="password" attribute is emitted
                //here. Recorded in migration-summary.md.
                field.InputType = "password";
            }

            if (disabled && !isPasswordField)
            {
                field.ReadOnly = true;
            }

            field.Value = defaultValue;

            if (!isNullable)
            {
                field.Label = label + Titles.RequiredFieldIndicator;
                field.AddRule(ScrudFactoryHelper.GetRequiredFieldValidator(field, errorCssClass));
                fieldTable.Add(field);
                return;
            }

            field.Label = label;
            fieldTable.Add(field);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetTextBox(string id, int maxLength)</c>, the single factory every text-ish
        ///     renderer (text, number, decimal, date) went through.
        ///     <para>
        ///         <c>ClientIDMode.Static</c> is gone with WebForms, but it is precisely what the
        ///         <see cref="ScrudField.ElementId" /> contract replaces: the element renders with
        ///         the unmangled id <c>GetFormCollection</c> reads it back by.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static ScrudField GetTextBox(string id, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            ScrudField field = new ScrudField();
            field.ElementId = id;
            field.Kind = ScrudFieldKind.TextBox;
            field.InputType = "text";

            if (maxLength > 0)
            {
                field.MaxLength = maxLength;
            }

            return field;
        }
    }
}
