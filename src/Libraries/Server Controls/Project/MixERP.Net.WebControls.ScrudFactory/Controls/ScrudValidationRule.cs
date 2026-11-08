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

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The rule kinds the generated form used to express with WebForms validator controls.
    ///     <para>
    ///         <c>Required</c> was <c>System.Web.UI.WebControls.RequiredFieldValidator</c>
    ///         (<c>ScrudFactoryHelper.GetRequiredFieldValidator</c>); <c>Date</c>, <c>Integer</c>
    ///         and <c>Currency</c> were <c>CompareValidator</c>s with
    ///         <c>ValidationDataType.Date</c> / <c>.Integer</c> / <c>.Currency</c>
    ///         (<c>ScrudDateTextBox.GetDateValidator</c>, <c>ScrudNumberTextBox.GetNumberValidator</c>,
    ///         <c>ScrudDecimalTextBox.GetDecimalValidator</c>); <c>RegularExpression</c> was the
    ///         image-extension <c>RegularExpressionValidator</c> in
    ///         <c>ScrudFileUpload.GetImageValidator</c>.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum ScrudValidationDataType
    {
        /// <summary>No type conversion is attempted — the former RequiredFieldValidator.</summary>
        None = 0,

        /// <summary>Former <c>ValidationDataType.Date</c>.</summary>
        Date = 1,

        /// <summary>Former <c>ValidationDataType.Integer</c>.</summary>
        Integer = 2,

        /// <summary>Former <c>ValidationDataType.Currency</c>.</summary>
        Currency = 3,

        /// <summary>Former <c>RegularExpressionValidator.ValidationExpression</c>.</summary>
        RegularExpression = 4
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Port of <c>System.Web.UI.WebControls.ValidationCompareOperator</c>, restricted to the
    ///     three members this project actually used.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum ScrudCompareOperator
    {
        /// <summary>
        ///     Former <c>ValidationCompareOperator.DataTypeCheck</c> — the value only has to be
        ///     convertible to <see cref="ScrudValidationRule.DataType" />; no comparison is made.
        /// </summary>
        DataTypeCheck = 0,

        /// <summary>Former <c>ValidationCompareOperator.GreaterThan</c>.</summary>
        GreaterThan = 1,

        /// <summary>Former <c>ValidationCompareOperator.GreaterThanEqual</c>.</summary>
        GreaterThanEqual = 2
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     One server-side validation rule attached to a generated form field — the replacement for
    ///     the WebForms validator controls the field renderers used to create and hand to
    ///     <c>ScrudFactoryHelper.AddRow</c>.
    ///     <para>
    ///         WHY THIS TYPE EXISTS. The original rules ran twice: as emitted client script
    ///         (<c>EnableClientScript = true</c> blocked the postback) and again server-side, with
    ///         <c>ScrudEvents.SaveButton_Click</c> gating on <c>Page.Validate()</c> /
    ///         <c>Page.IsValid</c>. Neither pipeline exists on net10.0, and
    ///         <c>CompareValidator</c> / <c>RegularExpressionValidator</c> /
    ///         <c>RequiredFieldValidator</c> are removed types. Dropping them would have let an
    ///         invalid or non-numeric row reach the database behind a green build, so every rule is
    ///         re-expressed here and <see cref="Evaluate" /> actually enforces it in C#, on the
    ///         server. This follows the precedent set by
    ///         <c>MixERP.Net.WebControls.Common.DateTextBox.Validate()</c> (see that project's
    ///         migration-summary.md §3a).
    ///     </para>
    ///     <para>
    ///         <see cref="ScrudField.Validate" /> / <see cref="ScrudFieldTable.Validate" /> are what
    ///         <c>ScrudForm</c>'s component-level <c>Validate()</c>/<c>IsValid</c> gate consumes in
    ///         place of <c>Page.Validate()</c>/<c>Page.IsValid</c>.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class ScrudValidationRule
    {
        /// <summary>
        ///     Former <c>validator.ID</c> — for example <c>"{column}_textboxRequiredValidator"</c>,
        ///     <c>"{column}_textboxCompareValidator"</c>, <c>"{column}_textboxNumberValidator"</c>,
        ///     <c>"{column}_textboxDecimalValidator"</c>, <c>"{column}_fileuploadRegexValidator"</c>.
        ///     The suffixes are preserved so the rendered message keeps the id the legacy DOM had.
        /// </summary>
        public string Id { get; set; }

        /// <summary>Former <c>BaseCompareValidator.Type</c> / the regular-expression marker.</summary>
        public ScrudValidationDataType DataType { get; set; }

        /// <summary>Former <c>CompareValidator.Operator</c>.</summary>
        public ScrudCompareOperator Operator { get; set; }

        /// <summary>Former <c>CompareValidator.ValueToCompare</c> (always <c>"0"</c> here).</summary>
        public string ValueToCompare { get; set; }

        /// <summary>Former <c>RegularExpressionValidator.ValidationExpression</c>.</summary>
        public string ValidationExpression { get; set; }

        /// <summary>
        ///     Former <c>validator.ErrorMessage</c>, carried VERBATIM including the leading
        ///     <c>&lt;br/&gt;</c> the originals prefixed, so the rendered message is unchanged.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>Former <c>validator.CssClass</c> (the form's <c>errorCssClass</c>).</summary>
        public string CssClass { get; set; }

        /// <summary>
        ///     True when this rule is a required-field rule. The required rule is the only one that
        ///     fires on a blank value — every <c>CompareValidator</c> and
        ///     <c>RegularExpressionValidator</c> treated an empty value as valid and deferred to the
        ///     <c>RequiredFieldValidator</c>. That deferral is preserved in <see cref="Evaluate" />.
        /// </summary>
        public bool IsRequiredRule
        {
            get { return this.DataType.Equals(ScrudValidationDataType.None); }
        }

        /// <summary>
        ///     Set by <see cref="Evaluate" />. Drives the message's visibility, which is the port of
        ///     <c>ValidatorDisplay.Dynamic</c>: the span is emitted only while the rule fails.
        /// </summary>
        public bool Failed { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Evaluates this rule against the field's current value and records the outcome in
        ///     <see cref="Failed" />.
        /// </summary>
        /// <param name="value">
        ///     The field's current value — the text the user typed, the selected list value, or the
        ///     posted file name for a file field.
        /// </param>
        /// <returns>True when the rule passes.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool Evaluate(string value)
        {
            this.Failed = false;

            bool empty = string.IsNullOrWhiteSpace(value);

            if (this.IsRequiredRule)
            {
                //RequiredFieldValidator: the value must not be blank.
                this.Failed = empty;
                return !this.Failed;
            }

            if (empty)
            {
                //CompareValidator / RegularExpressionValidator passed an empty value through and
                //left it to the RequiredFieldValidator. Preserved verbatim.
                return true;
            }

            switch (this.DataType)
            {
                case ScrudValidationDataType.Date:
                    this.Failed = !this.EvaluateDate(value);
                    break;
                case ScrudValidationDataType.Integer:
                    this.Failed = !this.EvaluateInteger(value);
                    break;
                case ScrudValidationDataType.Currency:
                    this.Failed = !this.EvaluateDecimal(value);
                    break;
                case ScrudValidationDataType.RegularExpression:
                    this.Failed = !this.EvaluateRegularExpression(value);
                    break;
            }

            return !this.Failed;
        }

        /// <summary>
        ///     Port of the date <c>CompareValidator</c> (<c>ValidationDataType.Date</c>,
        ///     <c>Operator = DataTypeCheck</c>): the value only has to be convertible to a date.
        /// </summary>
        private bool EvaluateDate(string value)
        {
            DateTime date;
            return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }

        /// <summary>
        ///     Port of the number <c>CompareValidator</c> (<c>ValidationDataType.Integer</c>,
        ///     <c>ValueToCompare = "0"</c>, operator <c>GreaterThan</c> for a MixERP "strict" domain
        ///     and <c>GreaterThanEqual</c> otherwise).
        /// </summary>
        private bool EvaluateInteger(string value)
        {
            long parsed;

            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                return false;
            }

            long compare;

            if (!long.TryParse(this.ValueToCompare, NumberStyles.Integer, CultureInfo.InvariantCulture, out compare))
            {
                //No comparand: a DataTypeCheck is all the rule asked for.
                return true;
            }

            return this.Compare(parsed.CompareTo(compare));
        }

        /// <summary>
        ///     Port of the decimal <c>CompareValidator</c> (<c>ValidationDataType.Currency</c>, same
        ///     <c>ValueToCompare = "0"</c> and strict/non-strict operator selection).
        /// </summary>
        private bool EvaluateDecimal(string value)
        {
            decimal parsed;

            //ValidationDataType.Currency accepted group separators and a currency symbol.
            const NumberStyles styles = NumberStyles.Currency;

            if (!decimal.TryParse(value, styles, CultureInfo.CurrentCulture, out parsed))
            {
                return false;
            }

            decimal compare;

            if (!decimal.TryParse(this.ValueToCompare, NumberStyles.Any, CultureInfo.InvariantCulture, out compare))
            {
                return true;
            }

            return this.Compare(parsed.CompareTo(compare));
        }

        /// <summary>Port of <c>RegularExpressionValidator</c>: the whole value must match.</summary>
        private bool EvaluateRegularExpression(string value)
        {
            if (string.IsNullOrWhiteSpace(this.ValidationExpression))
            {
                return true;
            }

            //RegularExpressionValidator anchored the match to the entire value.
            Match match = Regex.Match(value, this.ValidationExpression, RegexOptions.None, TimeSpan.FromSeconds(2));

            return match.Success && match.Index.Equals(0) && match.Length.Equals(value.Length);
        }

        /// <summary>Applies <see cref="Operator" /> to a <c>CompareTo</c> result.</summary>
        private bool Compare(int comparison)
        {
            switch (this.Operator)
            {
                case ScrudCompareOperator.GreaterThan:
                    return comparison > 0;
                case ScrudCompareOperator.GreaterThanEqual:
                    return comparison >= 0;
                default:
                    //DataTypeCheck: conversion succeeded, which is the whole rule.
                    return true;
            }
        }
    }
}
