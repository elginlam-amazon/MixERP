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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IO;
using System.Linq;

namespace MixERP.Net.Common.Helpers
{
    public static class JSONHelper
    {
        public static DataTable JsonToDataTable(string json)
        {
            return JsonConvert.DeserializeObject<DataTable>(json);
        }

        /// <summary>
        ///     Replacement for System.Web.Script.Serialization.JavaScriptSerializer.Deserialize&lt;dynamic&gt;,
        ///     which is not available on .NET. Produces the same untyped object graph:
        ///     objects become Dictionary&lt;string, object&gt;, arrays become object[], integers become
        ///     int (or long when they do not fit), and fractional numbers become decimal.
        /// </summary>
        public static object DeserializeDynamic(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using (JsonTextReader reader = new JsonTextReader(new StringReader(json)))
            {
                reader.FloatParseHandling = FloatParseHandling.Decimal;
                reader.DateParseHandling = DateParseHandling.None;
                return ToObject(JToken.ReadFrom(reader));
            }
        }

        private static object ToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return ((JObject) token).Properties().ToDictionary(p => p.Name, p => ToObject(p.Value));
                case JTokenType.Array:
                    return token.Select(ToObject).ToArray();
                case JTokenType.Integer:
                    long value = token.Value<long>();
                    if (value >= int.MinValue && value <= int.MaxValue)
                    {
                        return (int) value;
                    }

                    return value;
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                default:
                    return ((JValue) token).Value;
            }
        }
    }
}