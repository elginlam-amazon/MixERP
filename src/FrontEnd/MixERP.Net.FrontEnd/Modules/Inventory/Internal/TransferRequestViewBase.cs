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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Entities.Transactions;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Controls;
using MixERP.Net.Framework.Security;
using MixERP.Net.TransactionGovernor;
using MixERP.Net.WebControls.Flag;
using MixERP.Net.i18n.Resources;
using PetaPoco;

namespace MixERP.Net.Core.Modules.Inventory.Internal
{
    /// <summary>
    ///     The ported <c>TransferRequest.ascx.cs</c> code-behind, shared by the FOUR root-level
    ///     stock-transfer list screens exactly as the original shared it.
    ///     <para>
    ///         UPSTREAM SHAPE THIS REPRODUCES. <c>TransferRequest.ascx</c>,
    ///         <c>TransferAcknowledgement.ascx</c>, <c>TransferAuthorization.ascx</c> and
    ///         <c>TransferDelivery.ascx</c> all declared
    ///         <c>CodeBehind="TransferRequest.ascx.cs"</c>
    ///         <c>Inherits="MixERP.Net.Core.Modules.Inventory.TransferRequest"</c> - ONE
    ///         code-behind, four markup files. (The three siblings' own
    ///         <c>.ascx.designer.cs</c> files declared empty partial classes that nothing ever
    ///         bound to.) They differ ONLY in their heading, their toolbar buttons, the DEFAULT
    ///         values of the Authorized / Delivered / Received filters, and the javascript they
    ///         load. Putting the shared half here keeps that single-source-of-truth property:
    ///         the four screens cannot drift, which is what the one shared code-behind
    ///         guaranteed.
    ///     </para>
    ///     <para>
    ///         WHAT WAS PORTED. <c>AddGridView()</c> / <c>AddHiddenField()</c> /
    ///         <c>AddFlagControl()</c> built server controls that are MARKUP in each derived
    ///         <c>.razor</c> now (the migrated <c>MixERPGridView</c>, a plain hidden
    ///         <c>&lt;input&gt;</c> and the migrated <c>&lt;Flag&gt;</c>). The data query
    ///         (<c>GetDataSource</c>), the <c>Show</c> handler (<c>BindGridView</c>), the flag
    ///         update (<c>Flag_Updated</c>) and the selected-id parsing
    ///         (<c>GetSelectedValues</c>) are the real logic and live here, unchanged apart
    ///         from their value sources.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> return an EMPTY login
    ///         inside a Blazor circuit, so the four login-derived values come from
    ///         <see cref="MixERPUserControl.CurrentLogin" />.
    ///     </para>
    ///     <para>
    ///         The <c>IDisposable</c> implementation of the original released the WebForms
    ///         <c>MixERPGridView</c> / <c>FlagControl</c> / <c>HiddenField</c> CHILD CONTROL
    ///         instances. A Blazor component owns no child-control tree and no unmanaged
    ///         resource, so there is nothing to release and no empty <c>Dispose()</c> is
    ///         carried forward.
    ///     </para>
    /// </summary>
    public abstract class TransferRequestViewBase : MixERPUserControl
    {
        /// <summary>
        ///     The grid's rendered id. FROZEN: <c>wwwroot/js/TransferRequest.js</c> selects
        ///     <c>#TransferRequestGridView</c> by literal id on ALL FOUR screens (they all load
        ///     that one file), prepends its Actions and Select columns to it, and
        ///     <c>getSelectedItems()</c> reads the checkbox from column 2 and the id from
        ///     column 3 of it.
        /// </summary>
        public const string GridElementId = "TransferRequestGridView";

        /// <summary>
        ///     The hidden field <c>getSelectedItems()</c> writes the ticked ids into, and which
        ///     every one of the four screens' action buttons reads back. FROZEN id.
        /// </summary>
        public const string SelectedValuesElementId = "SelectedValuesHidden";

        /// <summary>The shared list script every one of the four screens loaded.</summary>
        protected const string TransferRequestScriptUrl =
            "_content/MixERP.Net.Core.Modules.Inventory/js/TransferRequest.js";

        private const string FlagResource = "transactions.inventory_transfer_requests";
        private const string FlagResourceKey = "inventory_transfer_request_id";

        private ElementValueReader elements;

        /// <summary>True once the login-derived values are available and the first query has run.</summary>
        protected bool Loaded { get; private set; }

        protected string Catalog { get; private set; }

        protected int UserId { get; private set; }

        protected long LoginId { get; private set; }

        /// <summary>
        ///     The office id the two DateTextBoxes are given - the original set
        ///     <c>DateFromDateTextBox.OfficeId</c> / <c>DateToDateTextBox.OfficeId</c> in
        ///     <c>OnControlLoad</c>.
        /// </summary>
        protected int OfficeId { get; private set; }

        /// <summary>The bound result set the grid renders (former <c>grid.DataSource</c>).</summary>
        protected IEnumerable<DbGetInventoryTransferRequestViewResult> Rows { get; private set; }

        //-------------------------------------------------------------------------------
        // The filter fields. Each was an <input runat="server"> (HtmlInputText) or a
        // DateTextBox whose .Value / .Text posted back; they are plain two-way bound fields
        // now. The three tri-state text filters keep their PER-SCREEN DEFAULTS, which is the
        // only server-side difference between the four screens - see each .razor.
        //-------------------------------------------------------------------------------

        protected string DateFromText { get; set; }

        protected string DateToText { get; set; }

        protected string Office { get; set; } = string.Empty;

        protected string Store { get; set; } = string.Empty;

        protected string Authorized { get; set; } = string.Empty;

        protected string Delivered { get; set; } = string.Empty;

        protected string Received { get; set; } = string.Empty;

        protected string User { get; set; } = string.Empty;

        protected string ReferenceNumber { get; set; } = string.Empty;

        protected string StatementReference { get; set; } = string.Empty;

        /// <summary>
        ///     The javascript files this screen loads, IN DOCUMENT ORDER. Order is load-bearing
        ///     for two of the four: their own script sets <c>window.checkListUrlOverride</c> /
        ///     <c>window.reportUrlOverride</c>, which <c>TransferRequest.js</c> reads.
        /// </summary>
        protected abstract string[] ScriptUrls { get; }

        /// <summary>
        ///     <see cref="MixERPUserControlBase.OnControlLoad" /> is abstract, so it is
        ///     satisfied here once for all four screens. The legacy body set the two
        ///     DateTextBox office ids and called AddGridView / BindGridView / AddFlagControl /
        ///     AddHiddenField; three of those four built server controls that are markup now,
        ///     and the fourth (BindGridView) plus the office id need the login, which is
        ///     asynchronous - so the work is in <see cref="OnInitializedAsync" />.
        /// </summary>
        public override void OnControlLoad(object sender, EventArgs e)
        {
        }

        protected override async Task OnInitializedAsync()
        {
            //base.OnInitializedAsync runs BOTH authorization checks - never skip it.
            await base.OnInitializedAsync().ConfigureAwait(true);

            LoginInfo login = await this.CurrentLogin.GetLoginAsync().ConfigureAwait(true);

            this.UserId = login.UserId;
            this.LoginId = login.LoginId;
            this.OfficeId = login.OfficeId;
            this.Catalog = await this.CurrentLogin.GetCatalogAsync().ConfigureAwait(true);

            //OnControlLoad called BindGridView() unconditionally, so the grid arrives populated.
            this.BindGridView();
            this.Loaded = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true);

            if (!firstRender)
            {
                return;
            }

            this.elements = new ElementValueReader(this.JSRuntime);

            //TransferRequest.js's $(window).load block reads #TransferRequestGridView out of
            //the DOM and rewrites every row, so it must run AFTER the grid has rendered - which
            //the first render guarantees, because OnInitializedAsync already bound the rows.
            await ModuleScripts.LoadAsync(this.JSRuntime, this.ScriptUrls).ConfigureAwait(true);
        }

        /// <summary>The IJSRuntime the derived component injects.</summary>
        protected abstract IJSRuntime JSRuntime { get; }

        /// <summary>Port of ShowButton_Click.</summary>
        protected void ShowButton_Click()
        {
            this.BindGridView();
        }

        /// <summary>
        ///     Port of BindGridView(). <c>DataBind()</c> has no counterpart - assigning the rows
        ///     re-renders.
        /// </summary>
        protected void BindGridView()
        {
            this.Rows = this.GetDataSource();
        }

        /// <summary>
        ///     Port of GetDataSource(), including the SQL and the exact parameter order, which
        ///     the 13 positional placeholders of
        ///     <c>transactions.get_inventory_transfer_request_view</c> depend on.
        /// </summary>
        private IEnumerable<DbGetInventoryTransferRequestViewResult> GetDataSource()
        {
            DateTime from = Conversion.TryCastDate(this.DateFromText);
            DateTime to = Conversion.TryCastDate(this.DateToText);

            const string sql =
                @"SELECT * FROM transactions.get_inventory_transfer_request_view(@0::integer, @1::bigint, @2::integer,@3::date,@4::date,@5::text,@6::text,@7::text,@8::text,@9::text,@10::text,@11::text,@12::text);";
            return Factory.Get<DbGetInventoryTransferRequestViewResult>(this.Catalog, sql, this.UserId,
                this.LoginId, this.OfficeId, from, to, this.Office, this.Store, this.Authorized,
                this.Delivered, this.Received, this.User, this.ReferenceNumber, this.StatementReference);
        }

        #region Flag

        /// <summary>
        ///     Port of Flag_Updated. The ticked ids come from <c>#SelectedValuesHidden</c>, which
        ///     <c>getSelectedItems()</c> fills client-side - under WebForms that value arrived on
        ///     the postback, so it is read back off the DOM here.
        /// </summary>
        protected async Task Flag_Updated(FlagUpdatedEventArgs e)
        {
            int flagTypeId = e.FlagId;

            const string resource = FlagResource;
            const string resourceKey = FlagResourceKey;

            //Preserved verbatim from the original. With `resource` a non-empty constant this
            //guard cannot fire, but it is the author's stated contract for the call below and
            //is carried across rather than quietly deleted.
            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new MixERPException(Warnings.CannotCreateFlagTransactionTableNull);
            }

            Collection<long> selected = await this.GetSelectedValuesAsync().ConfigureAwait(true);

            Flags.CreateFlag(this.Catalog, this.UserId, flagTypeId, resource, resourceKey,
                selected.Select(t => Conversion.TryCastString(t)).ToList().ToCollection());

            this.BindGridView();
        }

        /// <summary>
        ///     Port of GetSelectedValues(), including its own quirk: the collection is
        ///     <c>Collection&lt;long&gt;</c> but each value is parsed with
        ///     <c>TryCastInteger</c> and only kept when &gt; 0. Preserved as found.
        /// </summary>
        private async Task<Collection<long>> GetSelectedValuesAsync()
        {
            string selectedValues = this.elements == null
                ? null
                : await this.elements.GetValueAsync(SelectedValuesElementId).ConfigureAwait(true);

            //Check if something was selected.
            if (string.IsNullOrWhiteSpace(selectedValues))
            {
                return new Collection<long>();
            }

            //Create a collection object to store the IDs.
            Collection<long> values = new Collection<long>();

            //Iterate through each value in the selected values
            //and determine if each value is a number.
            foreach (string value in selectedValues.Split(','))
            {
                //Parse the value to integer.
                int val = Conversion.TryCastInteger(value);

                if (val > 0)
                {
                    values.Add(val);
                }
            }

            return values;
        }

        #endregion Flag
    }
}
