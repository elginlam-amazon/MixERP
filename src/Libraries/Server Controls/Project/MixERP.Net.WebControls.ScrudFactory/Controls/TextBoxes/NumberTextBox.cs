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
using System;

namespace MixERP.Net.WebControls.ScrudFactory.Controls.TextBoxes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the integral-number field (smallint / integer / bigint and their MixERP strict
    ///     domains) of the schema-driven scrud form.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudNumberTextBox
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddNumberTextBox(HtmlTable, …)</c>, branch for branch:
        ///     <list type="bullet">
        ///         <item>a <c>nextVal</c> default (a PostgreSQL sequence call, matched
        ///         case-insensitively) is NOT written into the field — the database supplies it;</item>
        ///         <item><c>disabled</c> makes the input read-only;</item>
        ///         <item>a SERIAL column is forced read-only and gets NO required rule, because the
        ///         original's <c>if (isSerial) … else { if (!isNullable) … }</c> structure skipped
        ///         the required validator and the indicator-suffixed label for it entirely;</item>
        ///         <item>a non-serial, non-nullable column gets the required rule and the
        ///         <c>Titles.RequiredFieldIndicator</c>-suffixed label.</item>
        ///     </list>
        ///     The number rule is attached in every case, as the original always passed
        ///     <c>numberValidator</c> to <c>AddRow</c>.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddNumberTextBox(ScrudFieldTable fieldTable, string resourceClassName, string columnName, string defaultValue, bool isSerial, bool isNullable, int maxLength, string domain, string errorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            ScrudField field = ScrudTextBox.GetTextBox(columnName + "_textbox", maxLength);

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.Kind = ScrudFieldKind.NumberTextBox;
            field.Domain = domain;
            field.IsNullable = isNullable;
            field.IsSerial = isSerial;
            field.ValidatorCssClass = errorCssClass;

            field.AddRule(GetNumberValidator(field, domain, errorCssClass));

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                if (!defaultValue.StartsWith("nextVal", StringComparison.OrdinalIgnoreCase))
                {
                    field.Value = defaultValue;
                }
            }

            field.ReadOnly = disabled;

            if (isSerial)
            {
                field.ReadOnly = true;
            }
            else
            {
                if (!isNullable)
                {
                    field.Label = label + Titles.RequiredFieldIndicator;
                    field.AddRule(ScrudFactoryHelper.GetRequiredFieldValidator(field, errorCssClass));
                    fieldTable.Add(field);
                    return;
                }
            }

            field.Label = label;
            fieldTable.Add(field);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetNumberValidator</c>. The <c>CompareValidator</c>
        ///     (<c>Type = ValidationDataType.Integer</c>, <c>ValueToCompare = "0"</c>,
        ///     <c>ErrorMessage = "&lt;br/&gt;" + Titles.OnlyNumbersAllowed</c>) is a removed type, so
        ///     the RULE is re-expressed as a <see cref="ScrudValidationRule" /> that
        ///     <see cref="ScrudField.Validate" /> enforces server-side.
        ///     <para>
        ///         The MixERP strict-domain switch is preserved exactly: a domain name ending in
        ///         <c>strict</c> compares <c>GreaterThan</c> (so zero is rejected), anything else
        ///         <c>GreaterThanEqual</c>.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudValidationRule GetNumberValidator(ScrudField fieldToValidate, string domain, string cssClass)
        {
            if (fieldToValidate == null)
            {
                return null;
            }

            ScrudValidationRule validator = new ScrudValidationRule();
            validator.Id = fieldToValidate.ElementId + "NumberValidator";
            validator.ErrorMessage = @"<br/>" + Titles.OnlyNumbersAllowed;
            validator.CssClass = cssClass;
            validator.DataType = ScrudValidationDataType.Integer;

            //MixERP strict data type
            if (domain != null && domain.EndsWith("strict", StringComparison.Ordinal))
            {
                validator.Operator = ScrudCompareOperator.GreaterThan;
            }
            else
            {
                validator.Operator = ScrudCompareOperator.GreaterThanEqual;
            }

            validator.ValueToCompare = "0";

            return validator;
        }
    }
}
