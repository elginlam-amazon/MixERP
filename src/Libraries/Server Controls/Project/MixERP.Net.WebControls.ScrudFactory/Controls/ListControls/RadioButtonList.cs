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

namespace MixERP.Net.WebControls.ScrudFactory.Controls.ListControls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the radio-button-list field of the schema-driven scrud form — in practice the
    ///     boolean columns, which <c>ScrudFactoryHelper.AddField</c> feeds with the
    ///     <c>Titles.Yes,Titles.No</c> / <c>true,false</c> key-value pair.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudRadioButtonList
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddRadioButtonList(HtmlTable, …)</c>: the
        ///     <c>{column}_radiobuttonlist</c> id (load-bearing — <c>GetFormCollection</c> reads the
        ///     value back by it), the <c>Enabled = !disabled</c> treatment, and the required rule
        ///     plus <c>Titles.RequiredFieldIndicator</c>-suffixed label for a non-nullable column.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddRadioButtonList(ScrudFieldTable fieldTable, string resourceClassName, string columnName, bool isNullable, string keys, string values, string selectedValue, string errorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            ScrudField field = GetRadioButtonList(columnName + "_radiobuttonlist", keys, values, selectedValue);

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = errorCssClass;

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            //RadioButtonList.Enabled = !disabled rendered the inputs with disabled="disabled".
            field.Disabled = disabled;

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
        ///     Port of <c>GetRadioButtonList(id, keys, values, selectedValues)</c>.
        ///     <para>
        ///         <c>ClientIDMode.Static</c> is replaced by the
        ///         <see cref="ScrudField.ElementId" /> contract, and
        ///         <c>RepeatDirection.Horizontal</c> — which made the control emit a single-row
        ///         table of radio/label pairs — is reproduced by <c>ScrudFieldRow.razor</c>'s
        ///         radio-button-list markup.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudField GetRadioButtonList(string id, string keys, string values, string selectedValues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            ScrudField field = new ScrudField();
            field.ElementId = id;
            field.Kind = ScrudFieldKind.RadioButtonList;

            Helper.AddListItems(field, keys, values, selectedValues);

            return field;
        }
    }
}
