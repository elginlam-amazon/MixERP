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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MixERP.Net.Web.UI.HtmlControls;

namespace MixERP.Net.Web.UI.WebControls
{
    public enum ValidatorDisplay
    {
        None = 0,
        Static = 1,
        Dynamic = 2
    }

    public enum ValidationDataType
    {
        String = 0,
        Integer = 1,
        Double = 2,
        Date = 3,
        Currency = 4
    }

    public enum ValidationCompareOperator
    {
        Equal = 0,
        NotEqual = 1,
        GreaterThan = 2,
        GreaterThanEqual = 3,
        LessThan = 4,
        LessThanEqual = 5,
        DataTypeCheck = 6
    }

    /// <summary>
    ///     Server-side validator. Registers with <see cref="Page.Validators" /> and renders its message only when invalid.
    /// </summary>
    public abstract class BaseValidator : Label, IValidator
    {
        protected BaseValidator()
        {
            this.IsValid = true;
        }

        public string ControlToValidate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidatorDisplay Display { get; set; } = ValidatorDisplay.Static;
        public bool EnableClientScript { get; set; } = true;
        public bool SetFocusOnError { get; set; }
        public string ValidationGroup { get; set; } = string.Empty;
        public bool IsValid { get; set; }

        public void Validate()
        {
            this.IsValid = !this.Visible || !this.Enabled || this.EvaluateIsValid();

            if (!this.IsValid && this.SetFocusOnError && this.Page != null)
            {
                Control target = this.FindTarget(this.ControlToValidate);

                if (target != null)
                {
                    this.Page.SetFocus(target.ClientID);
                }
            }
        }

        protected abstract bool EvaluateIsValid();

        protected override void OnInit(EventArgs e)
        {
            this.Page?.Validators.Add(this);
            base.OnInit(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            this.Page?.Validators.Remove(this);
            base.OnUnload(e);
        }

        protected string GetControlValidationValue(string name)
        {
            return GetValue(this.FindTarget(name));
        }

        protected Control FindTarget(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Control container = this.NamingContainer;

            while (container != null)
            {
                Control found = container.FindControl(id);

                if (found != null)
                {
                    return found;
                }

                container = container.NamingContainer;
            }

            return this.Page?.DescendantsAndSelf().FirstOrDefault(c => c.ID == id && !ReferenceEquals(c, this) && !(c is CompositeControl));
        }

        private static string GetValue(Control control)
        {
            switch (control)
            {
                case null:
                    return string.Empty;
                case TextBox textBox:
                    return textBox.Text;
                case HiddenField hidden:
                    return hidden.Value;
                case ListControl list:
                    return list.SelectedValue;
                case HtmlInputControl input:
                    return input.Value;
                case HtmlSelect select:
                    return select.Value;
                case HtmlTextArea textArea:
                    return textArea.Value;
                default:
                    TextBox inner = control.DescendantsAndSelf().OfType<TextBox>().FirstOrDefault();
                    return inner?.Text ?? string.Empty;
            }
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (this.IsValid)
            {
                if (this.Display == ValidatorDisplay.Dynamic || this.Display == ValidatorDisplay.None)
                {
                    this.Style["display"] = "none";
                }
                else
                {
                    this.Style["visibility"] = "hidden";
                }
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            if (this.Display == ValidatorDisplay.None)
            {
                return;
            }

            base.Render(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            writer.Write(string.IsNullOrEmpty(this.Text) ? this.ErrorMessage : this.Text);
        }

        internal static bool TryConvert(string text, ValidationDataType type, CultureInfo culture, out IComparable value)
        {
            value = null;

            switch (type)
            {
                case ValidationDataType.Integer:
                    if (int.TryParse(text, NumberStyles.Integer, culture, out int integer))
                    {
                        value = integer;
                        return true;
                    }

                    return false;
                case ValidationDataType.Double:
                    if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double number))
                    {
                        value = number;
                        return true;
                    }

                    return false;
                case ValidationDataType.Currency:
                    if (decimal.TryParse(text, NumberStyles.Currency, culture, out decimal currency))
                    {
                        value = currency;
                        return true;
                    }

                    return false;
                case ValidationDataType.Date:
                    if (DateTime.TryParse(text, culture, DateTimeStyles.None, out DateTime date))
                    {
                        value = date;
                        return true;
                    }

                    return false;
                default:
                    value = text;
                    return true;
            }
        }
    }

    public class RequiredFieldValidator : BaseValidator
    {
        public string InitialValue { get; set; } = string.Empty;

        protected override bool EvaluateIsValid()
        {
            string value = this.GetControlValidationValue(this.ControlToValidate) ?? string.Empty;
            return !string.Equals(value.Trim(), (this.InitialValue ?? string.Empty).Trim(), StringComparison.Ordinal);
        }
    }

    public abstract class BaseCompareValidator : BaseValidator
    {
        public ValidationDataType Type { get; set; }
        public bool CultureInvariantValues { get; set; }

        protected CultureInfo GetCulture()
        {
            return CultureInfo.CurrentCulture;
        }
    }

    public class CompareValidator : BaseCompareValidator
    {
        public ValidationCompareOperator Operator { get; set; }
        public string ValueToCompare { get; set; } = string.Empty;
        public string ControlToCompare { get; set; } = string.Empty;

        protected override bool EvaluateIsValid()
        {
            string value = this.GetControlValidationValue(this.ControlToValidate);

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            CultureInfo culture = this.GetCulture();

            if (!TryConvert(value.Trim(), this.Type, culture, out IComparable left))
            {
                return false;
            }

            if (this.Operator == ValidationCompareOperator.DataTypeCheck)
            {
                return true;
            }

            string other = string.IsNullOrEmpty(this.ControlToCompare) ? this.ValueToCompare : this.GetControlValidationValue(this.ControlToCompare);
            CultureInfo otherCulture = string.IsNullOrEmpty(this.ControlToCompare) && this.CultureInvariantValues ? CultureInfo.InvariantCulture : culture;

            if (!TryConvert((other ?? string.Empty).Trim(), this.Type, otherCulture, out IComparable right))
            {
                return true;
            }

            int comparison = left.CompareTo(right);

            switch (this.Operator)
            {
                case ValidationCompareOperator.Equal:
                    return comparison == 0;
                case ValidationCompareOperator.NotEqual:
                    return comparison != 0;
                case ValidationCompareOperator.GreaterThan:
                    return comparison > 0;
                case ValidationCompareOperator.GreaterThanEqual:
                    return comparison >= 0;
                case ValidationCompareOperator.LessThan:
                    return comparison < 0;
                case ValidationCompareOperator.LessThanEqual:
                    return comparison <= 0;
                default:
                    return true;
            }
        }
    }

    public class RangeValidator : BaseCompareValidator
    {
        public string MinimumValue { get; set; } = string.Empty;
        public string MaximumValue { get; set; } = string.Empty;

        protected override bool EvaluateIsValid()
        {
            string value = this.GetControlValidationValue(this.ControlToValidate);

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            CultureInfo culture = this.GetCulture();
            CultureInfo boundaryCulture = this.CultureInvariantValues ? CultureInfo.InvariantCulture : culture;

            if (!TryConvert(value.Trim(), this.Type, culture, out IComparable actual))
            {
                return false;
            }

            if (TryConvert(this.MinimumValue, this.Type, boundaryCulture, out IComparable min) && actual.CompareTo(min) < 0)
            {
                return false;
            }

            return !TryConvert(this.MaximumValue, this.Type, boundaryCulture, out IComparable max) || actual.CompareTo(max) <= 0;
        }
    }

    public class RegularExpressionValidator : BaseValidator
    {
        public string ValidationExpression { get; set; } = string.Empty;

        protected override bool EvaluateIsValid()
        {
            string value = this.GetControlValidationValue(this.ControlToValidate);

            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(this.ValidationExpression))
            {
                return true;
            }

            Match match = Regex.Match(value, this.ValidationExpression, RegexOptions.None, TimeSpan.FromSeconds(2));
            return match.Success && match.Index == 0 && match.Length == value.Length;
        }
    }

    public class ServerValidateEventArgs : EventArgs
    {
        public ServerValidateEventArgs(string value, bool isValid)
        {
            this.Value = value;
            this.IsValid = isValid;
        }

        public string Value { get; }
        public bool IsValid { get; set; }
    }

    public delegate void ServerValidateEventHandler(object source, ServerValidateEventArgs args);

    public class CustomValidator : BaseValidator
    {
        public event ServerValidateEventHandler ServerValidate;

        public string ClientValidationFunction { get; set; } = string.Empty;
        public bool ValidateEmptyText { get; set; }

        protected override bool EvaluateIsValid()
        {
            string value = string.IsNullOrEmpty(this.ControlToValidate) ? string.Empty : this.GetControlValidationValue(this.ControlToValidate);

            if (!this.ValidateEmptyText && !string.IsNullOrEmpty(this.ControlToValidate) && string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            ServerValidateEventArgs args = new ServerValidateEventArgs(value, true);
            this.ServerValidate?.Invoke(this, args);
            return args.IsValid;
        }
    }

    public class ValidationSummary : WebControl
    {
        public ValidationSummary() : base(HtmlTextWriterTag.Div)
        {
        }

        public string HeaderText { get; set; }
        public string ValidationGroup { get; set; }
        public bool ShowSummary { get; set; } = true;
        public bool ShowMessageBox { get; set; }

        protected internal override void Render(HtmlTextWriter writer)
        {
            if (!this.ShowSummary || this.Page == null)
            {
                return;
            }

            string[] messages = this.Page.Validators.OfType<BaseValidator>()
                .Where(v => !v.IsValid && (string.IsNullOrEmpty(this.ValidationGroup) || v.ValidationGroup == this.ValidationGroup))
                .Select(v => v.ErrorMessage)
                .Where(m => !string.IsNullOrEmpty(m))
                .ToArray();

            if (messages.Length == 0)
            {
                return;
            }

            this.RenderBeginTag(writer);

            if (!string.IsNullOrEmpty(this.HeaderText))
            {
                writer.Write(this.HeaderText);
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Ul);

            foreach (string message in messages)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Write(message);
                writer.RenderEndTag();
            }

            writer.RenderEndTag();
            this.RenderEndTag(writer);
        }
    }
}
