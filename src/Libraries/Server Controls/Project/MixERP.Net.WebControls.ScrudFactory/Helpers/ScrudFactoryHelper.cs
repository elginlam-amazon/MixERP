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
using MixERP.Net.WebControls.ScrudFactory.Controls;
using MixERP.Net.WebControls.ScrudFactory.Controls.ListControls;
using MixERP.Net.WebControls.ScrudFactory.Controls.TextBoxes;
using System.Linq;

namespace MixERP.Net.WebControls.ScrudFactory.Helpers
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The per-column dispatcher that makes the scrud form schema-driven: given one row of
    ///     PostgreSQL column metadata it decides which field renderer to use.
    ///     <para>
    ///         WHAT MOVED. The two row builders this class used to own —
    ///         <c>AddRow(HtmlTable, label, params Control[])</c> and
    ///         <c>AddDropDownList(HtmlTable, label, DropDownList, HtmlAnchor, RequiredFieldValidator)</c>
    ///         — existed only to assemble <c>HtmlTableRow</c>/<c>HtmlTableCell</c>/<c>Literal</c>
    ///         trees, all of which are removed types. Their MARKUP (the <c>label-cell</c> /
    ///         <c>control-cell</c> row, the <c>&lt;label for&gt;</c>, the nested
    ///         <c>role="item-selector-table"</c> table and its 24px selector cell) is ported to
    ///         <c>Controls/ScrudFieldRow.razor</c>, which renders one
    ///         <see cref="MixERP.Net.WebControls.ScrudFactory.Controls.ScrudField" />. The renderers
    ///         therefore append a field to a
    ///         <see cref="MixERP.Net.WebControls.ScrudFactory.Controls.ScrudFieldTable" /> instead of
    ///         mutating an <c>HtmlTable</c>, and the label composition each one performed (including
    ///         the <c>Titles.RequiredFieldIndicator</c> suffix) is unchanged.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudFactoryHelper
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddField(...)</c>. The DISPATCH ORDER and EVERY BRANCH are preserved,
        ///     because this is what makes the form schema-driven:
        ///     <list type="number">
        ///         <item>a SERIAL column is forced <c>disabled</c>;</item>
        ///         <item>a column with NO parent column is matched against <c>ScrudTypes</c> in the
        ///         original order — Strings, then Shorts/Integers/Longs, then Decimals, then Bools,
        ///         then Dates, then Files. The checks are independent <c>if</c>s, not an
        ///         <c>else if</c> chain, exactly as before (a data type that appeared in two of the
        ///         arrays would render two fields — no array overlaps today, but the structure is
        ///         kept rather than "tidied" into a chain that would silently change behaviour);</item>
        ///         <item>a column WITH a parent column is always a drop-down list, whatever its data
        ///         type — this is the foreign-key branch.</item>
        ///     </list>
        ///     Boolean columns are fed the <c>Titles.Yes,Titles.No</c> / <c>true,false</c> key-value
        ///     pair verbatim.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddField(string catalog, ScrudFieldTable fieldTable, string resourceClassName, string itemSelectorPath, string columnName, string defaultValue, bool isSerial, bool isNullable, string dataType, string domain, int maxLength, string parentTableSchema, string parentTable, string parentTableColumn, string displayFields, string displayViews, bool useDisplayFieldAsParent, string selectedValues, string errorCssClass, bool disabled)
        {
            if (isSerial)
            {
                disabled = true;
            }

            if (fieldTable == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(parentTableColumn))
            {
                if (ScrudTypes.Strings.Contains(dataType))
                {
                    ScrudTextBox.AddTextBox(fieldTable, resourceClassName, columnName, dataType, defaultValue, isNullable, maxLength, errorCssClass, disabled);
                }

                if (ScrudTypes.Shorts.Contains(dataType) || ScrudTypes.Integers.Contains(dataType) || ScrudTypes.Longs.Contains(dataType))
                {
                    ScrudNumberTextBox.AddNumberTextBox(fieldTable, resourceClassName, columnName, defaultValue, isSerial, isNullable, maxLength, domain, errorCssClass, disabled);
                }

                if (ScrudTypes.Decimals.Contains(dataType))
                {
                    ScrudDecimalTextBox.AddDecimalTextBox(fieldTable, resourceClassName, columnName, defaultValue, isNullable, maxLength, domain, errorCssClass, disabled);
                }

                if (ScrudTypes.Bools.Contains(dataType))
                {
                    ScrudRadioButtonList.AddRadioButtonList(fieldTable, resourceClassName, columnName, isNullable, Titles.Yes + "," + Titles.No, "true,false", defaultValue, errorCssClass, disabled);
                }

                if (ScrudTypes.Dates.Contains(dataType))
                {
                    ScrudDateTextBox.AddDateTextBox(fieldTable, resourceClassName, columnName, defaultValue, isNullable, errorCssClass, disabled);
                }

                if (ScrudTypes.Files.Contains(dataType))
                {
                    ScrudFileUpload.AddFileUpload(fieldTable, resourceClassName, columnName, isNullable, errorCssClass);
                }
            }
            else
            {
                ScrudDropDownList.AddDropDownList(catalog, fieldTable, resourceClassName, itemSelectorPath, columnName, isNullable, parentTableSchema, parentTable, parentTableColumn, defaultValue, displayFields, displayViews, useDisplayFieldAsParent, selectedValues, errorCssClass, disabled);
            }

            //The field now carries the PostgreSQL data type it was generated from, which is what
            //ScrudForm.GetFormCollection passes to ScrudParser.ParseValue(value, dataType). The
            //legacy code re-read the data type from the schema DataRow at collection time; keeping
            //it on the field removes that second lookup without changing the value parsed.
            ScrudField added = fieldTable.FindFieldByColumn(columnName);

            if (added != null && string.IsNullOrWhiteSpace(added.DataType))
            {
                added.DataType = dataType;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetRequiredFieldValidator(Control, string)</c>. The
        ///     <c>System.Web.UI.WebControls.RequiredFieldValidator</c> it returned is a removed type,
        ///     so the RULE is re-expressed as a
        ///     <see cref="MixERP.Net.WebControls.ScrudFactory.Controls.ScrudValidationRule" /> which
        ///     <see cref="MixERP.Net.WebControls.ScrudFactory.Controls.ScrudField.Validate" />
        ///     enforces in C# on the server — the replacement for the
        ///     <c>Page.Validate()</c>/<c>Page.IsValid</c> gate the save path used.
        ///     <para>
        ///         The validator id suffix (<c>RequiredValidator</c>), the css class and the error
        ///         message (<c>"&lt;br/&gt;" + Titles.RequiredField</c>, leading break included) are
        ///         carried verbatim, so the rendered message is unchanged.
        ///         <c>EnableClientScript</c> and <c>SetFocusOnError</c> have no modern counterpart
        ///         (see the Common precedent §2); <c>ValidatorDisplay.Dynamic</c> is reproduced by
        ///         <c>ScrudFieldRow.razor</c>, which emits the message span only while the rule fails.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static ScrudValidationRule GetRequiredFieldValidator(ScrudField fieldToValidate, string cssClass)
        {
            if (fieldToValidate == null)
            {
                return null;
            }

            ScrudValidationRule validator = new ScrudValidationRule();
            validator.Id = fieldToValidate.ElementId + "RequiredValidator";
            validator.ErrorMessage = @"<br/>" + Titles.RequiredField;
            validator.CssClass = cssClass;
            validator.DataType = ScrudValidationDataType.None;

            return validator;
        }
    }
}
