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

using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Which renderer a generated field uses — the replacement for "which WebForms control did
    ///     the factory new up". <c>ScrudFieldRow.razor</c> switches on this.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum ScrudFieldKind
    {
        /// <summary>Former <c>System.Web.UI.WebControls.TextBox</c> (<c>ScrudTextBox</c>).</summary>
        TextBox = 0,

        /// <summary>
        ///     Former <c>TextBox</c> + <c>jQueryUI.AddjQueryUIDatePicker</c>
        ///     (<c>ScrudDateTextBox</c>). Renders the already-migrated
        ///     <c>MixERP.Net.WebControls.Common.DateTextBox</c> component.
        /// </summary>
        DateTextBox = 1,

        /// <summary>Former numeric <c>TextBox</c> (<c>ScrudNumberTextBox</c>).</summary>
        NumberTextBox = 2,

        /// <summary>Former decimal <c>TextBox</c> (<c>ScrudDecimalTextBox</c>).</summary>
        DecimalTextBox = 3,

        /// <summary>Former <c>System.Web.UI.WebControls.DropDownList</c> (<c>ScrudDropDownList</c>).</summary>
        DropDownList = 4,

        /// <summary>Former <c>System.Web.UI.WebControls.RadioButtonList</c> (<c>ScrudRadioButtonList</c>).</summary>
        RadioButtonList = 5,

        /// <summary>Former <c>System.Web.UI.WebControls.FileUpload</c> (<c>ScrudFileUpload</c>).</summary>
        FileUpload = 6
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Port of <c>System.Web.UI.WebControls.ListItem</c>, restricted to the two members the list
    ///     renderers used (<c>Text</c>, <c>Value</c>) plus <c>Selected</c>.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class ScrudListItem
    {
        public ScrudListItem()
        {
        }

        public ScrudListItem(string text, string value)
        {
            this.Text = text;
            this.Value = value;
        }

        /// <summary>Former <c>ListItem.Text</c> — what the option displays.</summary>
        public string Text { get; set; }

        /// <summary>Former <c>ListItem.Value</c> — what is posted back.</summary>
        public string Value { get; set; }

        /// <summary>Former <c>ListItem.Selected</c>.</summary>
        public bool Selected { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Port of the item-selector <c>System.Web.UI.HtmlControls.HtmlAnchor</c> built by
    ///     <c>ScrudDropDownList.GetItemSelector</c>: an anchor that opens the item-selector popup for
    ///     a foreign-key column. Only the attribute values the original set are carried.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class ScrudItemSelectorLink
    {
        /// <summary>Former <c>Attributes["class"]</c> — the <c>ItemSelectorAnchorCssClass</c> scrud parameter.</summary>
        public string CssClass { get; set; }

        /// <summary>Former <c>Attributes["data-title"]</c> — the localized column name.</summary>
        public string Title { get; set; }

        /// <summary>Former <c>HtmlAnchor.HRef</c>, query string included and unchanged.</summary>
        public string Href { get; set; }

        /// <summary>
        ///     Former <c>Attributes.Add("style", "pointer-events:none;")</c>, added only when the
        ///     column was disabled.
        /// </summary>
        public bool Disabled { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     One row of the schema-driven scrud form: everything the legacy factory used to express by
    ///     newing up a WebForms control, setting its properties and adding it to an
    ///     <c>HtmlTable</c> cell.
    ///     <para>
    ///         The <see cref="ElementId" /> scheme is LOAD-BEARING. <c>Scrud.js</c> and
    ///         <c>ScrudForm.GetFormCollection</c> round-trip every value through the literal ids
    ///         <c>{column}_textbox</c>, <c>{column}_dropdownlist</c>,
    ///         <c>{column}_radiobuttonlist</c> and <c>{column}_fileupload</c>. Renaming one compiles
    ///         green and silently stops that column from reaching the database.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class ScrudField
    {
        private readonly Collection<ScrudValidationRule> rules = new Collection<ScrudValidationRule>();
        private readonly Collection<ScrudListItem> items = new Collection<ScrudListItem>();

        /// <summary>The PostgreSQL column this field is bound to (<c>column_name</c>).</summary>
        public string ColumnName { get; set; }

        /// <summary>
        ///     The rendered element's literal id and name — <c>{column}_textbox</c>,
        ///     <c>{column}_dropdownlist</c>, <c>{column}_radiobuttonlist</c> or
        ///     <c>{column}_fileupload</c>. This replaces the former
        ///     <c>ClientIDMode.Static</c> + <c>FindControl</c> pair.
        /// </summary>
        public string ElementId { get; set; }

        /// <summary>
        ///     The localized label, already suffixed with <c>Titles.RequiredFieldIndicator</c> when
        ///     the column is not nullable — exactly as the renderers composed it before handing it
        ///     to <c>ScrudFactoryHelper.AddRow</c>.
        /// </summary>
        public string Label { get; set; }

        /// <summary>Which renderer to use.</summary>
        public ScrudFieldKind Kind { get; set; }

        /// <summary>The PostgreSQL <c>data_type</c> this column carries, for <c>ScrudParser.ParseValue</c>.</summary>
        public string DataType { get; set; }

        /// <summary>The PostgreSQL <c>domain_name</c>, which selected the strict/non-strict comparison.</summary>
        public string Domain { get; set; }

        /// <summary>Former <c>TextBox.MaxLength</c>; emitted only when greater than zero.</summary>
        public int MaxLength { get; set; }

        /// <summary>The column's <c>is_nullable</c>. False adds the required rule.</summary>
        public bool IsNullable { get; set; }

        /// <summary>
        ///     The column is a PostgreSQL serial (its default is a <c>nextval</c> call). Serial
        ///     fields render read-only and carry no rules, as the original forced
        ///     <c>disabled = true</c> and skipped the required validator for them.
        /// </summary>
        public bool IsSerial { get; set; }

        /// <summary>
        ///     Former <c>TextBox.ReadOnly = true</c> / <c>Attributes.Add("disabled", "disabled")</c>
        ///     / <c>RadioButtonList.Enabled = false</c>, depending on the renderer.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        ///     Former <c>TextBox.ReadOnly</c>. A read-only input still posts its value; a disabled
        ///     one does not — the distinction the original drew between a disabled text box
        ///     (read-only) and a disabled drop-down (truly disabled) is preserved per renderer.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>Former <c>WebControl.CssClass</c> (for example <c>"date"</c>, <c>"decimal"</c>, <c>"color"</c>).</summary>
        public string CssClass { get; set; }

        /// <summary>
        ///     The <c>&lt;input&gt;</c> <c>type</c> attribute. <c>"password"</c> for a column whose
        ///     name contains PASSWORD, <c>"text"</c> otherwise.
        /// </summary>
        public string InputType { get; set; }

        /// <summary>
        ///     The field's current value: the text typed, the selected list value, or the posted
        ///     file name. This is what <c>ScrudForm.GetFormCollection</c> reads and passes to
        ///     <c>ScrudParser.ParseValue(value, dataType)</c> — the replacement for the four
        ///     <c>FindControl("{column}_…")</c> lookups.
        /// </summary>
        public string Value { get; set; }

        /// <summary>Former <c>validator.CssClass</c> source — the form's error css class.</summary>
        public string ValidatorCssClass { get; set; }

        /// <summary>
        ///     Former <c>ListControl.Items</c>, shared by the drop-down and radio-button renderers.
        /// </summary>
        public Collection<ScrudListItem> Items
        {
            get { return this.items; }
        }

        /// <summary>The item-selector anchor, or null when the column declares no display view.</summary>
        public ScrudItemSelectorLink ItemSelector { get; set; }

        /// <summary>
        ///     The browser file chosen for a <c>bytea</c> column, set by <c>ScrudFieldRow</c>'s
        ///     <c>InputFile</c> change handler. <c>ScrudFileUpload.UploadFileAsync</c> persists it.
        /// </summary>
        public IBrowserFile File { get; set; }

        /// <summary>
        ///     The server-side rules that replace this field's former validator controls. See
        ///     <see cref="ScrudValidationRule" />.
        /// </summary>
        public Collection<ScrudValidationRule> Rules
        {
            get { return this.rules; }
        }

        /// <summary>
        ///     True when every rule on this field passed the last <see cref="Validate" />. This is
        ///     one half of the replacement for <c>Page.IsValid</c>.
        /// </summary>
        public bool IsValid
        {
            get { return !this.rules.Any(rule => rule.Failed); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Evaluates every rule on this field against <see cref="Value" />, server-side.
        ///     Replaces the per-control half of the WebForms validation pipeline.
        /// </summary>
        /// <returns>True when the field is valid.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool Validate()
        {
            string value = this.Value;

            if (this.Kind.Equals(ScrudFieldKind.FileUpload))
            {
                //A FileUpload posted nothing until a file was chosen, and both its rules (required
                //and the image-extension regex) read the FILE NAME, not a typed value.
                value = this.File == null ? this.Value : this.File.Name;
            }

            bool valid = true;

            foreach (ScrudValidationRule rule in this.rules)
            {
                if (!rule.Evaluate(value))
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>Adds a rule, ignoring a null (the legacy factories could return null validators).</summary>
        public void AddRule(ScrudValidationRule rule)
        {
            if (rule == null)
            {
                return;
            }

            this.rules.Add(rule);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The generated form's field set — the replacement for the
    ///     <c>System.Web.UI.HtmlControls.HtmlTable</c> every renderer used to receive and mutate.
    ///     <para>
    ///         <c>ScrudForm.LoadForm</c> fills this through
    ///         <c>ScrudFactoryHelper.AddField(...)</c> and renders it as
    ///         <c>&lt;table role="scrud"&gt;</c>; <c>ScrudForm.GetFormCollection</c> reads values
    ///         back out of it by the same <c>{column}_…</c> element ids the legacy
    ///         <c>FindControl</c> calls used.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class ScrudFieldTable
    {
        private readonly Collection<ScrudField> fields = new Collection<ScrudField>();

        /// <summary>The fields, in the order the schema walk produced them (= the row order).</summary>
        public Collection<ScrudField> Fields
        {
            get { return this.fields; }
        }

        /// <summary>
        ///     True when every field passed the last <see cref="Validate" />. This is the value
        ///     <c>ScrudForm.IsValid</c> surfaces in place of <c>Page.IsValid</c>.
        /// </summary>
        public bool IsValid
        {
            //NOTE: the lambda parameter is deliberately NOT named "field" - that is a contextual
            //keyword inside a property accessor from C# 13 onwards.
            get { return this.fields.All(item => item.IsValid); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Evaluates every field's rules server-side. This is the replacement for
        ///     <c>Page.Validate()</c>, which <c>ScrudEvents.SaveButton_Click</c> and
        ///     <c>UseButton_Click</c> called before persisting.
        /// </summary>
        /// <returns>True when the whole form is valid.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool Validate()
        {
            bool valid = true;

            foreach (ScrudField field in this.fields)
            {
                if (!field.Validate())
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        ///     Looks a field up by its literal element id (<c>{column}_textbox</c>,
        ///     <c>{column}_dropdownlist</c>, <c>{column}_radiobuttonlist</c>,
        ///     <c>{column}_fileupload</c>) — the direct replacement for
        ///     <c>Control.FindControl(string)</c>.
        /// </summary>
        public ScrudField FindField(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                return null;
            }

            return this.fields.FirstOrDefault(field => string.Equals(field.ElementId, elementId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Looks a field up by the column it is bound to.</summary>
        public ScrudField FindFieldByColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return null;
            }

            return this.fields.FirstOrDefault(field => string.Equals(field.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Discards every field, so a re-opened form starts clean.</summary>
        public void Clear()
        {
            this.fields.Clear();
        }

        /// <summary>Appends a field, ignoring a null.</summary>
        public void Add(ScrudField field)
        {
            if (field == null)
            {
                return;
            }

            this.fields.Add(field);
        }

        /// <summary>
        ///     Every failed rule's message, for a consumer that wants to surface the form's errors
        ///     in one place (for example through <c>Alert.DisplayError</c>) in addition to the
        ///     per-field messages <c>ScrudFieldRow</c> renders.
        /// </summary>
        public IEnumerable<string> GetErrorMessages()
        {
            return this.fields.SelectMany(field => field.Rules)
                .Where(rule => rule.Failed)
                .Select(rule => rule.ErrorMessage);
        }
    }
}
