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
using System;
using System.Collections.Generic;
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
        ///     Deserializes JSON into the same untyped shape JavaScriptSerializer.Deserialize&lt;dynamic&gt; produced:
        ///     arrays become object[], objects become Dictionary&lt;string, object&gt;, and values become
        ///     string, bool, int, long, decimal or null.
        /// </summary>
        public static object DeserializeUntyped(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using (JsonTextReader reader = new JsonTextReader(new StringReader(json)))
            {
                reader.FloatParseHandling = FloatParseHandling.Decimal;
                reader.DateParseHandling = DateParseHandling.None;

                return ToUntyped(JToken.ReadFrom(reader));
            }
        }

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        private static object ToUntyped(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Array:
                    return token.Children().Select(ToUntyped).ToArray();
                case JTokenType.Object:
                    Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

                    foreach (JProperty property in ((JObject) token).Properties())
                    {
                        dictionary[property.Name] = ToUntyped(property.Value);
                    }

                    return dictionary;
                case JTokenType.Integer:
                    long value = token.Value<long>();

                    if (value >= int.MinValue && value <= int.MaxValue)
                    {
                        return (int) value;
                    }

                    return value;
                case JTokenType.Float:
                    return token.Value<decimal>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                default:
                    return token.Value<string>();
            }
        }
    }
}