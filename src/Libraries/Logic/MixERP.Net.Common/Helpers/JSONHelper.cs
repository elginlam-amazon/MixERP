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
using System.Collections.Generic;
using System.Data;
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
        /// Deserializes JSON into the same loosely typed shape JavaScriptSerializer.Deserialize&lt;dynamic&gt;() produced
        /// on .NET Framework: arrays become object[], objects become Dictionary&lt;string, object&gt; and values become CLR primitives.
        /// </summary>
        public static dynamic DeserializeDynamic(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return ToClrObject(JToken.Parse(json));
        }

        private static object ToClrObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Array:
                    return token.Select(ToClrObject).ToArray();
                case JTokenType.Object:
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();

                    foreach (JProperty property in ((JObject) token).Properties())
                    {
                        dictionary[property.Name] = ToClrObject(property.Value);
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
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                default:
                    return ((JValue) token).Value;
            }
        }
    }
}
