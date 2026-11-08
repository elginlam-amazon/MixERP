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
    ///     Builds the decimal field (numeric / money / currency and their MixERP strict domains) of
    ///     the schema-driven scrud form.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudDecimalTextBox
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddDecimalTextBox(HtmlTable, …)</c>: the default value, the read-only
        ///     treatment when disabled, the <c>decimal</c> css class, the domain-driven decimal rule,
        ///     and the required rule plus <c>Titles.RequiredFieldIndicator</c>-suffixed label for a
        ///     non-nullable column.
        ///     <para>
        ///         Note the original did NOT guard against a null <c>htmlTable</c> here (unlike its
        ///         siblings) and would have thrown inside <c>AddRow</c>; the guard is added because
        ///         <c>AddRow</c>'s own null check meant the row was silently skipped either way.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddDecimalTextBox(ScrudFieldTable fieldTable, string resourceClassName, string columnName, string defaultValue, bool isNullable, int maxLength, string domain, string errorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            ScrudField field = ScrudTextBox.GetTextBox(columnName + "_textbox", maxLength);

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.Kind = ScrudFieldKind.DecimalTextBox;
            field.Domain = domain;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = errorCssClass;

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            field.AddRule(GetDecimalValidator(field, domain, errorCssClass));

            field.Value = defaultValue;
            field.ReadOnly = disabled;
            field.CssClass = "decimal";

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
        ///     Port of <c>GetDecimalValidator</c>. The <c>CompareValidator</c>
        ///     (<c>Type = ValidationDataType.Currency</c>, <c>ValueToCompare = "0"</c>,
        ///     <c>ErrorMessage = "&lt;br/&gt;" + Titles.OnlyNumbersAllowed</c>) is a removed type, so
        ///     the RULE is re-expressed as a <see cref="ScrudValidationRule" /> enforced server-side
        ///     by <see cref="ScrudField.Validate" />. The strict-domain operator switch is preserved
        ///     exactly as the number validator's is.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudValidationRule GetDecimalValidator(ScrudField fieldToValidate, string domain, string cssClass)
        {
            if (fieldToValidate == null)
            {
                return null;
            }

            ScrudValidationRule validator = new ScrudValidationRule();
            validator.Id = fieldToValidate.ElementId + "DecimalValidator";
            validator.ErrorMessage = @"<br/>" + Titles.OnlyNumbersAllowed;
            validator.CssClass = cssClass;
            validator.DataType = ScrudValidationDataType.Currency;

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
