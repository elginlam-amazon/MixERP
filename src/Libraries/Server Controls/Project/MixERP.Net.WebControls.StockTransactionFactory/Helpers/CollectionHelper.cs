using MixERP.Net.Common;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace MixERP.Net.WebControls.StockTransactionFactory.Helpers
{
    public static class CollectionHelper
    {
        public static Collection<Attachment> GetAttachmentCollection(string json)
        {
            Collection<Attachment> details = new Collection<Attachment>();
            if (string.IsNullOrWhiteSpace(json)) return details;

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                if (result.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in result.EnumerateArray())
                    {
                        Attachment detail = new Attachment();
                        if (item.TryGetProperty("Comment", out var c)) detail.Comment = c.GetString();
                        if (item.TryGetProperty("FilePath", out var fp)) detail.FilePath = fp.GetString();
                        if (item.TryGetProperty("OriginalFileName", out var ofn)) detail.OriginalFileName = ofn.GetString();
                        details.Add(detail);
                    }
                }
            }
            catch { }

            return details;
        }

        public static Collection<StockDetail> GetStockMasterDetailCollection(string json, int storeId)
        {
            Collection<StockDetail> details = new Collection<StockDetail>();
            if (string.IsNullOrWhiteSpace(json)) return details;

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                if (result.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in result.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Array) continue;
                        var arr = item.EnumerateArray().ToArray();
                        StockDetail detail = new StockDetail();
                        if (arr.Length > 0) detail.ItemCode = arr[0].GetString();
                        if (arr.Length > 2) detail.Quantity = Conversion.TryCastInteger(arr[2].ToString());
                        if (arr.Length > 3) detail.UnitName = arr[3].GetString();
                        if (arr.Length > 4) detail.Price = Conversion.TryCastDecimal(arr[4].ToString());
                        if (arr.Length > 6) detail.Discount = Conversion.TryCastDecimal(arr[6].ToString());
                        if (arr.Length > 7) detail.ShippingCharge = Conversion.TryCastDecimal(arr[7].ToString());
                        if (arr.Length > 9) detail.TaxForm = arr[9].GetString();
                        if (arr.Length > 10) detail.Tax = Conversion.TryCastDecimal(arr[10].ToString());
                        detail.StoreId = storeId;
                        details.Add(detail);
                    }
                }
            }
            catch { }

            return details;
        }

        private static JsonElement[] ToArray(this JsonElement.ArrayEnumerator e)
        {
            var list = new System.Collections.Generic.List<JsonElement>();
            foreach (var item in e) list.Add(item);
            return list.ToArray();
        }
    }
}
