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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.JSInterop;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.ScrudFactory.Controls;
using MixERP.Net.WebControls.ScrudFactory.Data;
using MixERP.Net.WebControls.ScrudFactory.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace MixERP.Net.WebControls.ScrudFactory
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The FUNCTIONAL CORE of <c>ScrudForm</c> — the half of the component that was spread over
    ///     five of the seventeen legacy partial files:
    ///     <list type="bullet">
    ///         <item><c>ScrudForm/Events/ScrudEvents.cs</c> — the CRUD event pipeline
    ///         (<c>SaveButtonClick</c>/<c>UseButtonClick</c>, <c>SaveButton_Click</c>,
    ///         <c>UseButton_Click</c>, <c>Save(bool)</c>, <c>DeleteButton_Click</c>,
    ///         <c>EditButton_Click</c>, <c>CancelButton_Click</c>);</item>
    ///         <item><c>ScrudForm/Events/GridViewEvents.cs</c> — <c>FormGridView_RowDataBound</c>'s
    ///         per-row <c>#SelectRadio</c> injection, now a grid column
    ///         <see cref="MixERPGridViewColumn.Template" />;</item>
    ///         <item><c>ScrudForm/ViewFactory/Selection.cs</c> — <c>GetSelectedValue</c> /
    ///         <c>ClearSelectedValue</c>, now component state instead of a <c>FindControl</c> walk
    ///         of the grid's rows;</item>
    ///         <item><c>ScrudForm/FormFactory/FormFactory.cs</c> — <c>LoadForm</c> (the
    ///         schema-driven form generator) and <c>GetFormCollection</c> (the value round trip);</item>
    ///         <item><c>ScrudForm/FormFactory/Alert.cs</c> — <c>GetLocalizedErrorMessage</c> and
    ///         <c>ResetForm</c>;</item>
    ///         <item><c>ScrudForm/ScrudContainer.cs</c> — <c>AddScrudScript()</c>, plus the new
    ///         idempotent <c>initScrud()</c> invocation.</item>
    ///     </list>
    ///     <para>
    ///         THE VALIDATION GATE. <c>Page.Validate()</c> / <c>Page.IsValid</c> — the WebForms
    ///         pipeline the save path gated on — do not exist on net10.0. They are replaced by the
    ///         component's own <see cref="Validate" /> / <see cref="IsValid" /> pair, which runs
    ///         every rule on every generated field SERVER-SIDE through
    ///         <see cref="ScrudFieldTable.Validate" />. The gate is never bypassed: an invalid form
    ///         cannot reach <see cref="SaveAsync" />, exactly as an invalid page could not reach
    ///         <c>Save</c>.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public partial class ScrudForm : IDisposable
    {
        //==============================================================================================
        // ScrudForm/Events/ScrudEvents.cs — the two PUBLIC events (checklist task 53).
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Raised instead of performing the default save when a consumer has subscribed — the
        ///     port of <c>public event EventHandler SaveButtonClick</c>.
        ///     <para>
        ///         API DIVERGENCE (migration-summary §4): a C# <c>event EventHandler</c> cannot be
        ///         bound from Blazor markup and has no null-state a component can test, so this is
        ///         an <c>[Parameter] EventCallback</c> with the SAME NAME. The semantics are
        ///         preserved one for one: <see cref="EventCallback.HasDelegate" /> is the
        ///         replacement for the legacy <c>if (this.SaveButtonClick != null)</c> test, and
        ///         when it is set the default <see cref="SaveAsync" /> is NOT run — the consumer
        ///         owns the save, as before. The <c>(sender, e)</c> arguments are dropped because a
        ///         Blazor consumer receives the component instance it declared.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [Parameter]
        public EventCallback SaveButtonClick { get; set; }

        /// <summary>
        ///     Raised instead of performing the default save-and-close when a consumer has
        ///     subscribed — the port of <c>public event EventHandler UseButtonClick</c>. Same
        ///     divergence and same semantics as <see cref="SaveButtonClick" />.
        /// </summary>
        [Parameter]
        public EventCallback UseButtonClick { get; set; }

        //==============================================================================================
        // Injected services. These are [Inject] rather than @inject directives so that the markup
        // file stays the render tree and this file stays the logic.
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The replacement for <c>HttpContext.Current.Server.MapPath(...)</c> in the image
        ///     upload path: <c>ScrudFileUpload.UploadFileAsync</c> resolves the catalog's virtual
        ///     <c>TempMediaPath</c> against this environment's web root.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [Inject]
        private IWebHostEnvironment Environment { get; set; }

        //==============================================================================================
        // ScrudForm/ViewFactory/Selection.cs — selection state (checklist task 44).
        //
        // The legacy pair walked this.formGridView.Rows and cast row.Controls[0].Controls[0] to the
        // HtmlInputRadioButton the RowDataBound handler had injected, reading or clearing its
        // Checked property. There is no server-side control tree to walk on Blazor (and no
        // ViewState to carry the checked state across a postback), so the selected key lives here
        // and the radio's checked attribute is RENDERED FROM it — which is also what makes the
        // selection survive a re-render.
        //==============================================================================================

        /// <summary>
        ///     The key of the row whose <c>#SelectRadio</c> is checked, or <c>string.Empty</c> for
        ///     no selection. This IS the former <c>HtmlInputRadioButton.Checked</c>/<c>Value</c>
        ///     pair, hoisted to component state.
        /// </summary>
        private string selectedValue = string.Empty;

        /// <summary>
        ///     The interop handle published to <c>Scrud.js</c> so its row-click handler can call
        ///     <see cref="SelectRow" />. Released in <see cref="Dispose" />.
        /// </summary>
        private DotNetObjectReference<ScrudForm> selfReference;

        /// <summary>
        ///     What <c>#LastValueHidden</c> renders. The legacy <c>Save</c> wrote the selected key
        ///     (then the inserted key) into <c>this.lastValueHiddenTextBox.Text</c>; Scrud.js reads
        ///     it back in <c>scrudSaveAndClose()</c> to hand the saved key to a popup's opener.
        /// </summary>
        private string lastValue = string.Empty;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetSelectedValue()</c>: the checked row's value, or <c>string.Empty</c>.
        ///     Every branch of the CRUD pipeline keys off this — an empty value means "insert", a
        ///     non-empty one means "update/delete/edit THIS row".
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string GetSelectedValue()
        {
            return this.selectedValue ?? string.Empty;
        }

        /// <summary>
        ///     Port of <c>ClearSelectedValue()</c>: unchecks the grid's selection radio.
        /// </summary>
        private void ClearSelectedValue()
        {
            this.selectedValue = string.Empty;
        }

        /// <summary>
        ///     Records the row the user picked. This is the server-side half of
        ///     <c>scrudSelectRadioById()</c> — the click handler Scrud.js wires onto every grid row.
        /// </summary>
        private void SelectRadio_Changed(string value)
        {
            this.selectedValue = value ?? string.Empty;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The bridge that lets <c>Scrud.js</c>'s ROW-CLICK selection reach the server.
        ///     <para>
        ///         <c>scrudInitialize()</c> registers a click handler on every grid row which calls
        ///         <c>scrudSelectRadioById()</c> → jQuery <c>.prop("checked", true)</c>. jQuery
        ///         <c>.prop()</c> does NOT dispatch a <c>change</c> event, so the radio's Blazor
        ///         <c>@onchange</c> callback never fired and <see cref="selectedValue" /> stayed
        ///         empty — which silently turned every edit/delete into a no-op and forced
        ///         <c>SaveAsync</c> down the INSERT branch. In WebForms this worked because the
        ///         radio was a posted form field read back out of the control tree; Blazor has no
        ///         such postback, so the selection must be pushed to the component explicitly.
        ///     </para>
        ///     <para>
        ///         <c>Scrud.js</c> calls this through the <c>DotNetObjectReference</c> published in
        ///         <c>OnAfterRenderAsync</c>, passing the radio's <c>value</c> (the row key) — the
        ///         same string the <c>@onchange</c> path supplies, so both routes converge here.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [JSInvokable("SelectRow")]
        public void SelectRow(string value)
        {
            this.selectedValue = value ?? string.Empty;
            this.StateHasChanged();
        }

        //==============================================================================================
        // ScrudForm/Events/GridViewEvents.cs — FormGridView_RowDataBound (checklist task 43).
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>FormGridView_RowDataBound</c>'s <c>DataControlRowType.DataRow</c> branch.
        ///     <para>
        ///         The migrated <c>MixERPGridView</c> has no <c>RowDataBound</c> event (that was a
        ///         <c>GridView</c> member, and the whole point of the event here was to mutate the
        ///         server-side control tree of a row), so the per-row radio becomes the select
        ///         column's <see cref="MixERPGridViewColumn.Template" /> — which is the
        ///         <c>TemplateField</c> replacement the Common precedent prescribes.
        ///     </para>
        ///     <para>
        ///         THE <c>id="SelectRadio"</c> IS LOAD-BEARING. <c>Scrud.js</c>'s
        ///         <c>scrudInitialize()</c> finds <c>td input:radio</c> in each grid row and calls
        ///         <c>scrudSelectRadioById(radio.attr("id"))</c>, and
        ///         <c>scrudGetSelectedRadioValue()</c> reads <c>$("input[name=SelectRadio]:checked")</c>.
        ///         The legacy control set BOTH <c>Name</c> and <c>ID</c> to the literal
        ///         <c>SelectRadio</c> (it set <c>ClientIDMode.Static</c>, then
        ///         <c>Predictable</c> — the second assignment was dead because the id had already
        ///         been resolved, so the rendered id was the literal; that rendered output is what
        ///         is reproduced here).
        ///     </para>
        ///     <para>
        ///         The header branch of the same handler (localizing every header cell from
        ///         <c>Cells[1]</c> onward through <c>ScrudLocalizationHelper</c>) is NOT dropped:
        ///         it moved into <c>BuildGridColumns()</c>, which sets each column's
        ///         <c>HeaderText</c> from the same resource lookup — and skips the select column,
        ///         reproducing the <c>i = 1</c> offset.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private RenderFragment<object> GetSelectColumnTemplate()
        {
            return row => builder => this.BuildSelectRadio(builder, row);
        }

        private void BuildSelectRadio(RenderTreeBuilder builder, object row)
        {
            if (builder == null)
            {
                return;
            }

            string value = GetRowKeyValue(row);

            //  The id MUST be unique per row. Scrud.js selects the group with the prefix
            //  selectors $('[id^="SelectRadio"]') and $('[id^="SelectRadio"]:checked'), and
            //  scrudSelectRadioById() is handed the clicked row's OWN id, so a per-row suffix
            //  keeps every one of those selectors working while making $("#" + id) resolve the
            //  intended row. A single shared id made $("#SelectRadio") always resolve row 1.
            //  "name" stays exactly "SelectRadio" - it is the radio group, and the legacy
            //  posted-field contract.
            string radioId = "SelectRadio_" + GetRowIndex(row).ToString(CultureInfo.InvariantCulture);

            //  Port of selectField.ItemStyle.Width = 20 (GridPanel.cs). MixERPGridViewColumn has
            //  no Width/ItemStyle member, so the 20px is carried by a fixed-width inline-block
            //  wrapper: with the table's automatic layout the cell shrinks to its content, which
            //  reproduces the narrow select column. The "select-cell" class stays on the cell so
            //  host CSS can still target it.
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "style", "display:inline-block;width:20px;");

            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "radio");
            builder.AddAttribute(2, "id", radioId);
            builder.AddAttribute(3, "name", "SelectRadio");
            builder.AddAttribute(4, "value", value);
            builder.AddAttribute(5, "checked", string.Equals(value, this.selectedValue, StringComparison.Ordinal));
            builder.AddAttribute(6, "onchange",
                EventCallback.Factory.Create<ChangeEventArgs>(this, args => this.SelectRadio_Changed(value)));
            builder.CloseElement();

            builder.CloseElement();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The row's key value — the port of <c>e.Row.Cells[1].Text</c>, which the legacy
        ///     handler used as the radio's value.
        ///     <para>
        ///         Cell 0 was the select column itself, so <c>Cells[1]</c> was the FIRST column of
        ///         the view. That relationship is preserved literally (the first data column), and
        ///         it is why <c>BuildGridColumns()</c> keeps the select column first.
        ///         <c>FormHelper.GetView</c> orders by <c>KeyColumn</c> but selects <c>*</c>, so the
        ///         first column is whatever the view declares first — exactly the value the legacy
        ///         radio carried.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string GetRowKeyValue(object row)
        {
            DataRow dataRow = ResolveDataRow(row);

            if (dataRow == null || dataRow.Table.Columns.Count.Equals(0))
            {
                return string.Empty;
            }

            return Conversion.TryCastString(dataRow[0]);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Unwraps the grid's row object (a <see cref="DataRow" />, or a
        ///     <see cref="DataRowView" /> when the grid enumerates a <c>DefaultView</c>).
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static DataRow ResolveDataRow(object row)
        {
            DataRow dataRow = row as DataRow;

            if (dataRow == null)
            {
                DataRowView view = row as DataRowView;

                if (view != null)
                {
                    dataRow = view.Row;
                }
            }

            return dataRow;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The row's position in the bound table, used ONLY to give each select radio a unique
        ///     DOM id (<c>SelectRadio_{index}</c>).
        ///     <para>
        ///         The index is derived from the table rather than a render-time counter so the same
        ///         row keeps the same id across re-renders — a counter would have to be reset at a
        ///         render boundary the column template cannot observe.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static int GetRowIndex(object row)
        {
            DataRow dataRow = ResolveDataRow(row);

            if (dataRow == null || dataRow.Table == null)
            {
                return 0;
            }

            return dataRow.Table.Rows.IndexOf(dataRow);
        }

        //==============================================================================================
        // THE VALIDATION GATE (checklist task 65) — the replacement for Page.Validate()/Page.IsValid.
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Evaluates every rule on every generated field, server-side. This is the direct
        ///     replacement for <c>this.Page.Validate()</c>, which <c>SaveButton_Click</c> and
        ///     <c>UseButton_Click</c> called before persisting anything.
        ///     <para>
        ///         The rules themselves were WebForms validator controls
        ///         (<c>RequiredFieldValidator</c>, the date/integer/currency <c>CompareValidator</c>s
        ///         and the image-extension <c>RegularExpressionValidator</c>), all removed types.
        ///         They are re-expressed as <see cref="ScrudValidationRule" />s hanging off each
        ///         <see cref="ScrudField" /> and ENFORCED here in C# — not merely described as
        ///         <c>data-validate-*</c> attributes a client could ignore. The original's
        ///         blank-value deferral is preserved inside the rule: only the required rule fires
        ///         on an empty value.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Validate()
        {
            this.fieldTable.Validate();
        }

        /// <summary>
        ///     True when the last <see cref="Validate" /> found no failing rule — the replacement
        ///     for <c>this.Page.IsValid</c>.
        /// </summary>
        public bool IsValid
        {
            get { return this.fieldTable.IsValid; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Runs the gate and, when it fails, surfaces the failures.
        ///     <para>
        ///         The legacy gate was silent on the server: the per-field validator spans had
        ///         already been rendered by the client script, so <c>Page.IsValid</c> only had to
        ///         return. Here the per-field messages are still rendered by
        ///         <c>ScrudFieldRow</c> (<c>ValidatorDisplay.Dynamic</c>'s port), and the collected
        ///         messages are additionally written into <c>#ScrudError</c> so a user whose browser
        ///         never ran the client validators still sees why the save was refused.
        ///     </para>
        /// </summary>
        /// <returns>True when the form may be persisted.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool PassesValidationGate()
        {
            this.Validate();

            if (this.IsValid)
            {
                return true;
            }

            string message = string.Join(string.Empty, this.fieldTable.GetErrorMessages());

            this.messageCssClass = this.GetErrorCssClass();
            this.messageText = message;
            this.messageStyle = "display:block;font-size:16px;padding:8px 0;";

            return false;
        }

        //==============================================================================================
        // ScrudForm/Events/ScrudEvents.cs — the CRUD pipeline.
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>SaveButton_Click</c> (checklist task 54). The structure is unchanged:
        ///     validate, bail out when invalid, hand over to a subscribed consumer, otherwise save.
        ///     <para>
        ///         <c>this.Page.Validate(); if (!this.Page.IsValid) { return; }</c> becomes the
        ///         component's own gate. THE GATE IS NOT OPTIONAL — without it an invalid row would
        ///         reach <c>FormHelper.InsertRecord</c>, which is exactly what the WebForms
        ///         pipeline existed to prevent.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private async Task SaveButton_Click()
        {
            if (!this.PassesValidationGate())
            {
                return;
            }

            if (this.SaveButtonClick.HasDelegate)
            {
                //The legacy "if (this.SaveButtonClick != null) { raise; return; }" branch: a
                //consumer that subscribed owns the save entirely.
                await this.SaveButtonClick.InvokeAsync().ConfigureAwait(false);
                return;
            }

            await this.SaveAsync(false).ConfigureAwait(false);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>UseButton_Click</c> (checklist task 55) — the modal branch's
        ///     save-and-hand-back. Same gate, same consumer-override semantics, then
        ///     <c>Save(true)</c>.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private async Task UseButton_Click()
        {
            if (!this.PassesValidationGate())
            {
                return;
            }

            if (this.UseButtonClick.HasDelegate)
            {
                await this.UseButtonClick.InvokeAsync().ConfigureAwait(false);
                return;
            }

            await this.SaveAsync(true).ConfigureAwait(false);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>Save(bool closeForm)</c> — the whole point of the control. EVERY branch of
        ///     the original is here (checklist task 56):
        ///     <list type="number">
        ///         <item>the <c>userId &lt;= 0</c> guard throwing <c>Errors.InvalidUserId</c>;</item>
        ///         <item><c>GetFormCollection(true)</c>, which skips the serial column;</item>
        ///         <item><c>#LastValueHidden</c> primed with the selected key;</item>
        ///         <item>INSERT when nothing is selected — <c>DenyAdd</c> guard,
        ///         <c>FormHelper.InsertRecord</c> with the <c>imageColumn</c>, the inserted key into
        ///         <c>#LastValueHidden</c>, the form regenerated, the <c>ReturnUrl</c> redirect, the
        ///         grid refresh and <c>DisplaySuccess</c>;</item>
        ///         <item>UPDATE when a row is selected — <c>DenyEdit</c> guard, the comma-split
        ///         upper-cased <c>ExcludeEdit</c> exclusion array, <c>FormHelper.UpdateRecord</c>
        ///         with the <c>imageColumn</c>, the form regenerated, the grid refreshed, and the
        ///         <c>Titles.UnknownError</c> failure branch;</item>
        ///         <item><c>DisplayError</c> on any <c>MixERPException</c>, which also resets the
        ///         form.</item>
        ///     </list>
        ///     <para>
        ///         <c>closeForm</c> was unused in the original (its "ReSharper disable once
        ///         UnusedParameter.Local" says so explicitly — closing was done client-side by
        ///         <c>scrudSaveAndClose()</c> reading <c>#LastValueHidden</c>). It is kept so the
        ///         two call sites stay distinguishable and so the client contract is unchanged.
        ///     </para>
        ///     <para>
        ///         The method is <c>async</c> only because the image column's upload is
        ///         (<c>IBrowserFile</c> streams asynchronously); no branch was added or removed.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private async Task SaveAsync(bool closeForm)
        {
            int userId = this.UserId;

            if (userId <= 0)
            {
                throw new InvalidOperationException(Errors.InvalidUserId);
            }

            this.busy = true;

            try
            {
                Collection<KeyValuePair<string, object>> list =
                    await this.GetFormCollectionAsync(true).ConfigureAwait(false);

                string id = this.GetSelectedValue();
                this.lastValue = id;

                if (string.IsNullOrWhiteSpace(id))
                {
                    try
                    {
                        if (this.DenyAdd)
                        {
                            //Enforced SERVER-side, not merely by hiding the Add affordance.
                            throw new MixERPException(Titles.AccessIsDenied);
                        }

                        long lastInsertedValue = FormHelper.InsertRecord(this.Catalog, userId, this.TableSchema,
                            this.Table, this.KeyColumn, list, this.imageColumn);

                        if (lastInsertedValue > 0)
                        {
                            this.lastValue = lastInsertedValue.ToString(CultureInfo.InvariantCulture);

                            //"this.formContainer.Controls.Clear()" + LoadForm(container, table):
                            //the generated fields are discarded and rebuilt empty.
                            this.ReloadEmptyForm();

                            if (this.RedirectToReturnUrl())
                            {
                                return;
                            }

                            //Refresh the grid.
                            this.ForceGridRefresh();
                            this.BindGridView();
                            this.DisplaySuccess();
                        }
                    }
                    catch (MixERPException ex)
                    {
                        this.DisplayError(ex);
                    }
                }
                else
                {
                    try
                    {
                        if (this.DenyEdit)
                        {
                            throw new MixERPException(Titles.AccessIsDenied);
                        }

                        string[] exclusion = { "" };

                        if (!string.IsNullOrWhiteSpace(this.ExcludeEdit))
                        {
                            exclusion = this.ExcludeEdit.Split(',').Select(x => x.Trim().ToUpperInvariant()).ToArray();
                        }

                        if (FormHelper.UpdateRecord(this.Catalog, userId, this.TableSchema, this.Table, list,
                            this.KeyColumn, id, this.imageColumn, exclusion))
                        {
                            this.ReloadEmptyForm();

                            //Refresh the grid.
                            this.ForceGridRefresh();
                            this.BindGridView();

                            this.DisplaySuccess();
                        }
                        else
                        {
                            this.DisplayError(new MixERPException(Titles.UnknownError));
                        }
                    }
                    catch (MixERPException ex)
                    {
                        this.DisplayError(ex);
                    }
                }
            }
            finally
            {
                this.busy = false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>Save</c>'s post-insert redirect (checklist task 57):
        ///     <c>if (this.Page.Request.QueryString["ReturnUrl"] != null) { this.Page.Response.Redirect(...); return; }</c>
        ///     <para>
        ///         SECURITY FIX, DELIBERATE. The original passed a raw, client-supplied query-string
        ///         value straight to <c>Response.Redirect</c> — an open redirect: a crafted link
        ///         such as <c>…?ReturnUrl=https://evil.example/login</c> would bounce an
        ///         authenticated user to an attacker's site immediately after a successful save,
        ///         which is a textbook phishing primitive. The redirect behaviour is PRESERVED for
        ///         every legitimate (application-local) value and REFUSED for an off-site one, which
        ///         is logged. This vulnerability is not carried forward; recorded in
        ///         migration-summary.md.
        ///     </para>
        /// </summary>
        /// <returns>True when a redirect was issued, which ends the save exactly as the original did.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool RedirectToReturnUrl()
        {
            string returnUrl = this.GetQueryStringValue("ReturnUrl");

            if (returnUrl == null)
            {
                //The original tested for null, NOT for empty - preserved.
                return false;
            }

            if (!IsLocalUrl(returnUrl))
            {
                Serilog.Log.Warning("ScrudFactory: refused a non-local ReturnUrl {ReturnUrl}.", returnUrl);
                return false;
            }

            this.Nav.NavigateTo(returnUrl);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     A url is local when it addresses this application and cannot leave its origin: a
        ///     single leading '/' that is not followed by another '/' or a '\' (which browsers treat
        ///     as protocol-relative, i.e. off-site), and no scheme or authority of its own.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("/\\", StringComparison.Ordinal))
            {
                //Protocol-relative: "//evil.example" leaves the origin.
                return false;
            }

            if (url.StartsWith("/", StringComparison.Ordinal))
            {
                return true;
            }

            //Anything carrying a scheme or an authority is off-site; a relative path such as
            //"Index.aspx?x=1" stays inside the application and is allowed, as it was before.
            return !url.Contains("://") && !url.Contains(":") && !url.StartsWith("\\", StringComparison.Ordinal);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>DeleteButton_Click</c> (checklist task 58): nothing selected is a no-op,
        ///     the <c>DenyDelete</c> policy throws <c>Titles.AccessIsDenied</c> SERVER-side, and a
        ///     successful delete refreshes the grid and reports success. <c>DenyDelete</c> also
        ///     suppresses the button, but this check is what actually enforces it — a crafted
        ///     request cannot delete a row the policy forbids.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Task DeleteButton_Click()
        {
            string id = this.GetSelectedValue();

            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.CompletedTask;
            }

            this.busy = true;

            try
            {
                if (this.DenyDelete)
                {
                    throw new MixERPException(Titles.AccessIsDenied);
                }

                if (FormHelper.DeleteRecord(this.Catalog, this.TableSchema, this.Table, this.KeyColumn, id))
                {
                    //Refresh the grid.
                    this.ClearSelectedValue();
                    this.ForceGridRefresh();
                    this.BindGridView();

                    this.DisplaySuccess();
                }
            }
            catch (MixERPException ex)
            {
                this.DisplayError(ex);
            }
            finally
            {
                this.busy = false;
            }

            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>EditButton_Click</c> (checklist task 59): the selected row is read back
        ///     out of the table and loaded into the form in EDIT mode (which is what applies the
        ///     <c>ExcludeEdit</c> → <c>disabled</c> treatment), the grid is hidden and the form is
        ///     shown.
        ///     <para>
        ///         The <c>DenyEdit</c> guard is enforced here as well as on the affordance, and the
        ///         <c>CustomFormUrl</c> branch is preserved: a consumer that supplied one is
        ///         redirected to it with <c>KeyColumn</c> and the selected value appended as query
        ///         string, instead of the built-in form. (The legacy control performed that
        ///         redirect client-side, in <c>Scrud.js</c>'s <c>scrudEdit()</c>, off the injected
        ///         <c>customFormUrl</c> global — which is still emitted — so the behaviour now
        ///         holds whichever side initiates the edit.) The url is validated the same way the
        ///         <c>ReturnUrl</c> redirect is.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Task EditButton_Click()
        {
            string id = this.GetSelectedValue();

            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.CompletedTask;
            }

            try
            {
                if (this.DenyEdit)
                {
                    throw new MixERPException(Titles.AccessIsDenied);
                }

                if (!string.IsNullOrWhiteSpace(this.CustomFormUrl))
                {
                    string url = this.CustomFormUrl +
                                 (this.CustomFormUrl.Contains("?") ? "&" : "?") +
                                 this.KeyColumn + "=" + Uri.EscapeDataString(id);

                    if (IsLocalUrl(url))
                    {
                        this.Nav.NavigateTo(url);
                    }
                    else
                    {
                        Serilog.Log.Warning("ScrudFactory: refused a non-local CustomFormUrl {Url}.", url);
                    }

                    return Task.CompletedTask;
                }

                using (DataTable table = FormHelper.GetTable(this.Catalog, this.TableSchema, this.Table,
                    this.KeyColumn, id, this.KeyColumn))
                {
                    if (table.Rows.Count.Equals(1))
                    {
                        //"this.formContainer.Controls.Clear()" then LoadForm(container, table, true):
                        //the fields are regenerated from the row's values, in edit mode.
                        this.LoadForm(table, true);

                        this.gridVisible = false;
                        this.formVisible = true;
                    }
                }
            }
            catch (MixERPException ex)
            {
                this.DisplayError(ex);
            }

            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>CancelButton_Click</c> (checklist task 60): the form's generated fields
        ///     are discarded, the grid selection is cleared and an empty form is generated again.
        ///     <para>
        ///         The panel visibility is deliberately NOT changed here, exactly as in the
        ///         original: Scrud.js owns that half of the interaction (the ESC shortcut clicks
        ///         <c>#CancelButton</c> through the injected <c>"cancelButtonId "</c> global, and
        ///         its own handler hides the form), so flipping it server-side would fight the
        ///         client script.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Task CancelButton_Click()
        {
            //Clear grid selection.
            this.ClearSelectedValue();

            //Clear the form, and load the form again.
            this.ReloadEmptyForm();

            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The repeated "clear the form container, then <c>LoadForm</c> an empty
        ///     <c>DataTable</c> whose <c>Locale</c> is the current culture" sequence the legacy
        ///     pipeline performed in four places. Factored out so the four call sites cannot drift,
        ///     not changed.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void ReloadEmptyForm()
        {
            using (DataTable table = new DataTable())
            {
                table.Locale = Thread.CurrentThread.CurrentCulture;
                this.LoadForm(table, false);
            }
        }

        /// <summary>
        ///     Discards <c>BindGridView</c>'s memo so the next call really re-queries the view.
        ///     The legacy control bound the grid once per request, so a post-save
        ///     <c>BindGridView()</c> always hit the database; the memo (added by the shell to keep
        ///     <c>OnParametersSet</c> from re-querying on every parameter change) has to be
        ///     invalidated explicitly at the points where the data actually changed.
        /// </summary>
        private void ForceGridRefresh()
        {
            this.gridKey = null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Re-runs the whole-form gate after a single field changed, so a message that the user
        ///     has just fixed disappears and a newly broken field reports itself — the server-side
        ///     equivalent of the per-control client validators that used to re-evaluate on change.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Task OnFieldChanged(ScrudField field)
        {
            if (field != null && !string.IsNullOrWhiteSpace(this.imageColumn) &&
                field.Kind.Equals(ScrudFieldKind.FileUpload))
            {
                //A newly chosen file replaces whatever was staged for the image column.
                this.imageColumn = field.ColumnName;
            }

            this.Validate();

            if (this.IsValid)
            {
                //Clear a stale gate message once the form is clean again.
                if (this.messageText != null && this.messageText.Contains("<br/>"))
                {
                    this.messageText = string.Empty;
                    this.messageStyle = null;
                }
            }

            return Task.CompletedTask;
        }

        //==============================================================================================
        // ScrudForm/FormFactory/Alert.cs (checklist tasks 23 and 25).
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetLocalizedErrorMessage(MixERPException)</c>, VERBATIM (checklist task
        ///     23). A PostgreSQL constraint violation carries the constraint's name, which MixERP
        ///     uses as a resource key so that, for example, a unique-index violation reads as a
        ///     localized sentence instead of a database error. When the catalog's resource class
        ///     has no such key the lookup throws <see cref="MissingManifestResourceException" />,
        ///     which is swallowed and the raw <c>ex.Message</c> returned — the fallback is kept
        ///     because it is the only thing standing between the user and a blank message.
        ///     <para>
        ///         CULTURE. <c>i18n.ResourceManager.GetString(resourceClass, resourceKey,
        ///         cultureCode = null)</c> resolves against the CURRENT UI culture when no culture
        ///         code is passed, which is what the legacy two-argument call relied on. The call
        ///         is left two-argument so the behaviour is identical; the culture is named here
        ///         only to document it.
        ///     </para>
        ///     <para>
        ///         The sibling fallbacks the same pipeline uses — <c>Titles.UnknownError</c> when
        ///         an update reports no rows, <c>Titles.AccessIsDenied</c> for the three
        ///         <c>Deny*</c> policies, <c>Errors.InvalidUserId</c> for the save guard and
        ///         <c>Errors.KeyValueMismatch</c> in the list-item builder — are all preserved at
        ///         their own call sites.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string GetLocalizedErrorMessage(MixERPException ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            try
            {
                //CultureInfo.CurrentUICulture is what the resource manager falls back to.
                return i18n.ResourceManager.GetString(this.GetResourceClassName(), ex.DBConstraintName);
            }
            catch (MissingManifestResourceException)
            {
                //swallow
            }

            return ex.Message;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>ResetForm()</c> (checklist task 25). Only the registration channel
        ///     changes: <c>PageUtility.RegisterJavascript(key, script, Page, true)</c> becomes the
        ///     <see cref="IJSRuntime" /> overload of the same method.
        ///     <para>
        ///         THE <c>#form1</c> SELECTOR IS VERBATIM. It is the host page's form id (the same
        ///         one <c>Scrud.js</c>'s <c>scrudAddNew()</c> resets), so changing it to something
        ///         "cleaner" would silently stop clearing the form after a save.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void ResetForm()
        {
            PageUtility.RegisterJavascript("ScrudFormReset", "$('#form1').each(function(){this.reset();});",
                this.JSRuntime, true);
        }

        //==============================================================================================
        // ScrudForm/FormFactory/FormFactory.cs (checklist tasks 62 and 63).
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>LoadForm(Panel container, DataTable values, bool editing)</c> (checklist
        ///     task 62) — the schema-driven form generator, and the reason this control exists.
        ///     <para>
        ///         The walk is unchanged: <c>TableHelper.GetTable(Catalog, TableSchema, Table,
        ///         Exclude)</c> returns one row per (non-excluded) column, and the same NINE schema
        ///         columns are read off it — <c>column_name</c>, <c>column_default</c>,
        ///         <c>is_nullable</c>, <c>data_type</c>, <c>domain_name</c>,
        ///         <c>character_maximum_length</c>, <c>references_schema</c>,
        ///         <c>references_table</c>, <c>references_field</c>. The <c>nextval</c> serial
        ///         detection (<c>StringComparison.OrdinalIgnoreCase</c>) and the case-insensitive,
        ///         comma-split, trimmed <c>ExcludeEdit</c> → <c>disabled</c> logic are preserved,
        ///         as is the single-row <c>values</c> override that turns an insert form into an
        ///         edit form.
        ///     </para>
        ///     <para>
        ///         WHAT CHANGED. The <c>HtmlTable</c> the factories mutated (and the
        ///         <c>container.Controls.Add</c> that published it) are removed types: the fields
        ///         are collected into <see cref="ScrudFieldTable" /> and the markup renders them as
        ///         the same <c>&lt;table role="scrud"&gt;</c>, one <c>ScrudFieldRow</c> per field.
        ///         The former <c>container.Controls.Clear()</c> that every caller performed first
        ///         is now this method's own <c>Clear()</c>, so a regenerated form cannot end up
        ///         with duplicated fields.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void LoadForm(DataTable values, bool editing)
        {
            this.fieldTable.Clear();

            using (DataTable table = TableHelper.GetTable(this.Catalog, this.TableSchema, this.Table, this.Exclude))
            {
                if (table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        string columnName = Conversion.TryCastString(row["column_name"]);
                        string defaultValue = Conversion.TryCastString(row["column_default"]);
                        bool isSerial = defaultValue.StartsWith("nextval", StringComparison.OrdinalIgnoreCase);
                        bool isNullable = Conversion.TryCastBoolean(row["is_nullable"]);
                        string dataType = Conversion.TryCastString(row["data_type"]);
                        string domain = Conversion.TryCastString(row["domain_name"]);
                        int maxLength = Conversion.TryCastInteger(row["character_maximum_length"]);

                        string parentTableSchema = Conversion.TryCastString(row["references_schema"]);
                        string parentTable = Conversion.TryCastString(row["references_table"]);
                        string parentTableColumn = Conversion.TryCastString(row["references_field"]);

                        if (values != null && values.Rows.Count.Equals(1))
                        {
                            defaultValue = Conversion.TryCastString(values.Rows[0][columnName]);
                        }

                        bool disabled = false;

                        if (editing)
                        {
                            if (!string.IsNullOrWhiteSpace(this.ExcludeEdit))
                            {
                                if (this.ExcludeEdit.Split(',')
                                    .Any(column => column.Trim()
                                        .ToUpperInvariant()
                                        .Equals(columnName.ToUpperInvariant())))
                                {
                                    disabled = true;
                                }
                            }
                        }

                        ScrudFactoryHelper.AddField(this.Catalog, this.fieldTable, this.GetResourceClassName(),
                            this.GetItemSelectorPath(), columnName, defaultValue, isSerial, isNullable, dataType,
                            domain, maxLength, parentTableSchema, parentTable, parentTableColumn, this.DisplayFields,
                            this.DisplayViews, this.UseDisplayViewsAsParents, this.SelectedValues,
                            this.GetErrorCssClass(), disabled);
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetFormCollection(bool skipSerial)</c> (checklist task 63): iterates the
        ///     table's columns, reads each generated field's current value, and returns the same
        ///     <c>Collection&lt;KeyValuePair&lt;string, object&gt;&gt;</c> of column name → parsed
        ///     value that <c>FormHelper.InsertRecord</c>/<c>UpdateRecord</c> consume.
        ///     <para>
        ///         THE FOUR <c>FindControl</c> LOOKUPS KEEP THEIR ID STRINGS. The original located
        ///         each value with
        ///         <c>this.formContainer.FindControl(columnName + "_textbox" / "_radiobuttonlist" /
        ///         "_fileupload" / "_dropdownlist")</c>; <c>FindControl</c> is gone, so the keyed
        ///         <see cref="ScrudFieldTable.FindField" /> is used with the SAME four strings — the
        ///         ids the field renderers emit and the client scripts select on. Every branch
        ///         still runs its value through
        ///         <c>ScrudParser.ParseValue(value, dataType)</c>, and the <c>bytea</c> branch still
        ///         assigns <see cref="imageColumn" />, which is what tells
        ///         <c>FormHelper.InsertRecord</c>/<c>UpdateRecord</c> which column holds a file
        ///         path rather than a literal.
        ///     </para>
        ///     <para>
        ///         DELIBERATE DROP: the four <c>using</c> blocks. The original wrapped each
        ///         <c>FindControl</c> RESULT in a <c>using</c>, disposing a control it did not own
        ///         and that was still part of the page's control tree — a latent bug (it disposed
        ///         the live form's own inputs mid-request, and the only reason it was not visibly
        ///         broken is that the controls were never rendered again afterwards). There is
        ///         nothing to dispose about a <see cref="ScrudField" />, so the blocks are gone
        ///         rather than reproduced; recorded in migration-summary.md.
        ///     </para>
        ///     <para>
        ///         ASYNC: the method is <c>async</c> because the <c>bytea</c> branch's upload is.
        ///         <c>FileUpload.SaveAs</c> was synchronous; <c>IBrowserFile</c> streams. The
        ///         RETURN TYPE's element shape is unchanged, which is what the frozen
        ///         <c>FormHelper</c> contract cares about.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private async Task<Collection<KeyValuePair<string, object>>> GetFormCollectionAsync(bool skipSerial)
        {
            Collection<KeyValuePair<string, object>> list = new Collection<KeyValuePair<string, object>>();

            using (DataTable table = TableHelper.GetTable(this.Catalog, this.TableSchema, this.Table, this.Exclude))
            {
                if (table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        string columnName = Conversion.TryCastString(row["column_name"]);
                        string defaultValue = Conversion.TryCastString(row["column_default"]);
                        bool isSerial = defaultValue.StartsWith("nextval", StringComparison.OrdinalIgnoreCase);
                        string parentTableColumn = Conversion.TryCastString(row["references_field"]);
                        string dataType = Conversion.TryCastString(row["data_type"]);

                        if (skipSerial)
                        {
                            if (isSerial)
                            {
                                continue;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(parentTableColumn))
                        {
                            if (ScrudTypes.TextBoxTypes.Contains(dataType))
                            {
                                //FindControl(columnName + "_textbox")
                                ScrudField textBox = this.fieldTable.FindField(columnName + "_textbox");

                                if (textBox != null)
                                {
                                    list.Add(new KeyValuePair<string, object>(columnName,
                                        ScrudParser.ParseValue(textBox.Value, dataType)));
                                }
                            }

                            if (ScrudTypes.Bools.Contains(dataType))
                            {
                                //FindControl(columnName + "_radiobuttonlist")
                                ScrudField radioButtonList =
                                    this.fieldTable.FindField(columnName + "_radiobuttonlist");

                                if (radioButtonList != null)
                                {
                                    list.Add(new KeyValuePair<string, object>(columnName,
                                        ScrudParser.ParseValue(radioButtonList.Value, dataType)));
                                }
                            }

                            if (dataType.Equals("bytea"))
                            {
                                //FindControl(columnName + "_fileupload")
                                ScrudField fileUpload = this.fieldTable.FindField(columnName + "_fileupload");

                                if (fileUpload != null)
                                {
                                    string file = await ScrudFileUpload
                                        .UploadFileAsync(this.Catalog, this.Environment, fileUpload.File)
                                        .ConfigureAwait(false);

                                    list.Add(new KeyValuePair<string, object>(columnName, file));
                                    this.imageColumn = columnName;
                                }
                            }
                        }
                        else
                        {
                            //DropDownList - FindControl(columnName + "_dropdownlist")
                            ScrudField dropDownList = this.fieldTable.FindField(columnName + "_dropdownlist");

                            object value = null;

                            if (dropDownList != null && !string.IsNullOrWhiteSpace(dropDownList.Value))
                            {
                                value = dropDownList.Value;
                            }

                            //The pair is added even when the control was missing or empty, so the
                            //column is explicitly set to NULL - preserved verbatim.
                            list.Add(new KeyValuePair<string, object>(columnName, value));
                        }
                    }
                }
            }

            return list;
        }

        //==============================================================================================
        // ScrudForm/ScrudContainer.cs AddScrudScript() (checklist tasks 80 and 81).
        //==============================================================================================

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddScrudScript()</c> (checklist task 80).
        ///     <para>
        ///         WHY <c>OnAfterRenderAsync(firstRender)</c> AND NOT <c>OnInitialized</c>: the
        ///         script's <c>$(document).ready</c> body binds handlers to
        ///         <c>#FormGridView</c>, <c>#GridPanel</c>, <c>#FormPanel</c> and friends, so the
        ///         elements must already be in the DOM when it runs. In WebForms the registration
        ///         happened while the control tree was being built and the browser only saw it
        ///         after the whole page had rendered, which is exactly the guarantee
        ///         <c>OnAfterRenderAsync(firstRender: true)</c> gives.
        ///     </para>
        ///     <para>
        ///         THE IDS ARE PLAIN LITERALS NOW. The original passed
        ///         <c>this.formGridView.ID</c>, <c>this.gridPanel.ID</c> and so on; every one of
        ///         those controls was created with <c>ClientIDMode.Static</c>, so <c>.ID</c> and the
        ///         rendered client id were the same string. There is no <c>UniqueID</c> mangling to
        ///         compensate for in Blazor, so the literals are passed directly — the SAME values
        ///         the client script has always received through the twelve injected globals.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void AddScrudScript()
        {
            string script = ScrudJavascriptHelper.GetScript(this.Catalog, this.KeyColumn, this.CustomFormUrl,
                "FormGridView", "GridPanel", "UserIdHidden", "OfficeCodeHidden", "TitleLabel", "FormPanel",
                "CancelButton");

            PageUtility.RegisterJavascript("ScrudFormScript", script, this.JSRuntime, true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Registers <c>Scrud.js</c> once, then (re)wires its handlers after EVERY render
        ///     (checklist task 81).
        ///     <para>
        ///         <c>Scrud.js</c> bound its handlers from <c>$(document).ready</c> and re-bound
        ///         them from <c>Sys.WebForms.PageRequestManager.add_endRequest</c> — the ASP.NET
        ///         AJAX "a partial postback just replaced some DOM" hook. Neither fires after a
        ///         Blazor re-render, so a grid the component re-rendered would come back with dead
        ///         row-click handlers. <c>initScrud()</c> is the idempotent entry point added to
        ///         <c>Scrud.js</c> for exactly that: it is the modern replacement for
        ///         <c>add_endRequest</c>, and it un-binds before it binds so repeated calls cannot
        ///         stack duplicate handlers.
        ///     </para>
        ///     <para>
        ///         The <see cref="JSException" /> guard is required on the first render:
        ///         <c>PageUtility.RegisterJavascript</c> is a fire-and-forget interop call (its
        ///         signature is <c>void</c> and frozen), so <c>initScrud</c> may not be defined yet
        ///         when this runs. Missing it once is harmless — the script's own
        ///         <c>$(document).ready</c> performs the first wiring.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                this.AddScrudScript();
            }

            try
            {
                //  Hand Scrud.js a reference to this component so its row-click selection can
                //  reach the server (see SelectRow). Created once and reused; disposed with the
                //  component.
                if (this.selfReference == null)
                {
                    this.selfReference = DotNetObjectReference.Create(this);
                }

                await this.JSRuntime.InvokeVoidAsync("initScrud", this.selfReference).ConfigureAwait(false);
            }
            catch (JSException)
            {
                //Not registered yet (first render) - Scrud.js's own $(document).ready wires it.
            }
            catch (InvalidOperationException)
            {
                //Interop is unavailable during prerendering; the next render performs the wiring.
            }

            await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Releases the interop reference published to <c>Scrud.js</c>.
        ///     <para>
        ///         This is NOT a revival of the 169-LOC WebForms <c>Dispose</c> tree (that nulled
        ///         child controls and is correctly gone). A
        ///         <see cref="DotNetObjectReference{TValue}" /> is a genuine unmanaged-style
        ///         interop handle that the JS side holds, so it must be released or the circuit
        ///         leaks it for the lifetime of the connection.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (this.selfReference != null)
            {
                this.selfReference.Dispose();
                this.selfReference = null;
            }
        }
    }
}
