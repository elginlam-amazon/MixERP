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

using MixERP.Net.Entities;
using MixERP.Net.Entities.Models.Transactions;
using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MixERP.Net.WebControls.StockTransactionViewFactory.Data.Models
{
    public sealed class MergeModel
    {
        private Collection<long> transactionIdCollection = new Collection<long>();
        private Collection<ProductDetail> view = new Collection<ProductDetail>();
        public int AgentId { get; set; }
        public TranBook Book { get; set; }
        public bool NonTaxableSales { get; set; }
        public string PartyCode { get; set; }
        public int PriceTypeId { get; set; }
        public string ReferenceNumber { get; set; }
        public int SalesPersonId { get; set; }
        public string ShippingAddressCode { get; set; }
        public int ShippingCompanyId { get; set; }
        public string StatementReference { get; set; }
        public int StoreId { get; set; }
        public SubTranBook SubBook { get; set; }

        /// <summary>
        /// The merged transaction ids.
        /// </summary>
        /// <remarks>
        /// The setter exists SOLELY so that System.Text.Json can populate this collection when the
        /// model is round-tripped as JSON through ProtectedSessionStorage (key "Product") between
        /// StockTransactionViewFactory (writer) and StockTransactionFactory (reader). A get-only
        /// collection property is silently skipped by System.Text.Json, which used to lose every
        /// merged transaction id. The setter never stores null so that readers such as
        /// string.Join(",", model.TransactionIdCollection) stay safe.
        /// </remarks>
        [JsonInclude]
        public Collection<long> TransactionIdCollection
        {
            get { return this.transactionIdCollection; }
            set { this.transactionIdCollection = value ?? new Collection<long>(); }
        }

        public DateTime ValueDate { get; set; }

        /// <summary>
        /// The merged product lines.
        /// </summary>
        /// <remarks>
        /// Settable for the same System.Text.Json round-trip reason as
        /// <see cref="TransactionIdCollection"/>; never null.
        /// </remarks>
        [JsonInclude]
        public Collection<ProductDetail> View
        {
            get { return this.view; }
            set { this.view = value ?? new Collection<ProductDetail>(); }
        }

        public void AddTransactionIdToCollection(long transactionId)
        {
            this.transactionIdCollection.Add(transactionId);
        }

        public void AddViewToCollection(ProductDetail product)
        {
            this.view.Add(product);
        }
    }
}