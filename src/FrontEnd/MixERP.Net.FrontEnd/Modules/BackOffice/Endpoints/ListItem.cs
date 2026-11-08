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

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The replacement for <c>System.Web.UI.WebControls.ListItem</c>, which the former
    ///     <c>Services/Receipt/Accounts.asmx</c> and <c>Services/Receipt/Currencies.asmx</c>
    ///     returned as <c>Collection&lt;ListItem&gt;</c>.
    ///     <para>
    ///         THE JSON SHAPE IS THE CONTRACT. <c>mixerp-core.js</c>'s <c>bindAjaxData</c> - which
    ///         is what <c>wwwroot/js/entry/Receipt.js</c> uses for the currency, cost-centre,
    ///         cash-repository, bank-account and payment-card selects - reads
    ///         <c>this.Value</c> and <c>this.Text</c> off every element of <c>msg.d</c>. Those two
    ///         PascalCase names, serialized by Newtonsoft without a naming policy, are therefore
    ///         byte-identical to what the ASMX produced for the properties the caller actually
    ///         reads, so no javascript response handling changes.
    ///     </para>
    ///     <para>
    ///         The constructor argument order mirrors
    ///         <c>new ListItem(text, value)</c> so the ported service code reads exactly as the
    ///         original did.
    ///     </para>
    /// </summary>
    public sealed class ListItem
    {
        public ListItem()
        {
        }

        public ListItem(string text, string value)
        {
            this.Text = text;
            this.Value = value;
        }

        public string Text { get; set; }

        public string Value { get; set; }
    }
}
