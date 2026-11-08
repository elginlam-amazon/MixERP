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

using MixERP.Net.Common;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MixERP.Net.WebControls.StockTransactionFactory.Helpers
{
    public static class CollectionHelper
    {
        /// <summary>
        /// The attachment payload the client posts. The member NAMES are a contract with the
        /// JSON the attachment widget sends ("Comment", "FilePath", "OriginalFileName") and are
        /// matched case-sensitively, exactly as the former
        /// JavaScriptSerializer.Deserialize&lt;dynamic&gt;() dictionary lookups were.
        /// </summary>
        private sealed class AttachmentPayload
        {
            [JsonPropertyName("Comment")]
            public string Comment { get; set; }

            [JsonPropertyName("FilePath")]
            public string FilePath { get; set; }

            [JsonPropertyName("OriginalFileName")]
            public string OriginalFileName { get; set; }
        }

        public static Collection<Attachment> GetAttachmentCollection(string json)
        {
            Collection<Attachment> details = new Collection<Attachment>();

            List<AttachmentPayload> result = JsonSerializer.Deserialize<List<AttachmentPayload>>(json);

            //Null-result guard preserved verbatim from the JavaScriptSerializer implementation:
            //a JSON "null" body yields a null list and must produce an empty collection.
            if (result != null)
            {
                foreach (AttachmentPayload item in result)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    Attachment detail = new Attachment();
                    detail.Comment = item.Comment;
                    detail.FilePath = item.FilePath;
                    detail.OriginalFileName = item.OriginalFileName;

                    details.Add(detail);
                }
            }

            return details;
        }

        public static Collection<StockDetail> GetStockMasterDetailCollection(string json, int storeId)
        {
            Collection<StockDetail> details = new Collection<StockDetail>();

            //The payload is an ARRAY OF ARRAYS produced by tableToJSON() over #ProductGridView.
            //The POSITIONAL indices below are a load-bearing contract with that JS and with the
            //12-column grid order (ItemCode, ItemName, Quantity, Unit, Price, Amount, Discount,
            //ShippingCharge, SubTotal, TaxForm, Tax, Action). Indices 1, 5, 8 and 11 are
            //deliberately NOT read - do not "fill them in".
            //NOTE: the original (JavaScriptSerializer) implementation had NO null guard here and
            //threw on a null payload; this port keeps the same "throw on a null/empty body"
            //behaviour - JsonDocument.Parse rejects an empty body and a JSON "null" root throws
            //on enumeration, exactly as before (recorded in migration-summary.md §4).
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    StockDetail detail = new StockDetail();
                    detail.ItemCode = GetValue(item, 0);
                    detail.Quantity = Conversion.TryCastInteger(GetValue(item, 2));
                    detail.UnitName = GetValue(item, 3);
                    detail.Price = Conversion.TryCastDecimal(GetValue(item, 4));
                    detail.Discount = Conversion.TryCastDecimal(GetValue(item, 6));
                    detail.ShippingCharge = Conversion.TryCastDecimal(GetValue(item, 7));
                    detail.TaxForm = GetValue(item, 9);
                    detail.Tax = Conversion.TryCastDecimal(GetValue(item, 10));
                    detail.StoreId = storeId;

                    details.Add(detail);
                }
            }

            return details;
        }

        /// <summary>
        /// Reads the cell at the supplied position of a row array as a string. tableToJSON emits
        /// strings, but a number or null is tolerated the same way the old dynamic indexer was.
        /// </summary>
        private static string GetValue(JsonElement row, int index)
        {
            if (index >= row.GetArrayLength())
            {
                return null;
            }

            JsonElement cell = row[index];

            switch (cell.ValueKind)
            {
                case JsonValueKind.String:
                    return cell.GetString();
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return cell.ToString();
            }
        }
    }
}
