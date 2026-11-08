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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The transport parity layer for this module's five former
    ///     <c>Services/*.asmx</c> and <c>Services/Entry/*.asmx</c> <c>[ScriptService]</c> web
    ///     services.
    ///     <para>
    ///         THE ENVELOPE IS PRESERVED, byte for byte, exactly as the host's
    ///         <c>Endpoints/AsmxCompatibility.cs</c> preserves it for its own five host services: a
    ///         <c>ScriptService</c> wrapped every return value in <c>{ "d": &lt;value&gt; }</c> and
    ///         every unhandled exception in <c>{ "Message", "StackTrace", "ExceptionType" }</c> with
    ///         status 500. <c>wwwroot/js/entry/*.js</c> read <c>msg.d</c> through
    ///         <c>mixerp-core.js</c>'s <c>getAjax</c> / <c>ajaxDataBind</c> / <c>ajaxUpdateVal</c>,
    ///         and report failures through <c>getAjaxErrorMessage</c>, which reads
    ///         <c>JSON.parse(xhr.responseText).Message</c>. Keeping the envelope is what makes the
    ///         javascript change URL-ONLY.
    ///     </para>
    ///     <para>
    ///         WHY THIS IS A SECOND COPY AND NOT A REFERENCE. This project is a Razor Class Library
    ///         that the HOST references; referencing the host back is the inverted direction that
    ///         makes an RCL's components undiscoverable, and it is exactly what the infrastructure
    ///         pass removed. The host's copy therefore cannot be reused from here. The two are kept
    ///         deliberately identical in behaviour - any change to one must be mirrored.
    ///     </para>
    ///     <para>
    ///         NEWTONSOFT, NOT System.Text.Json: the callers index CLR property names
    ///         (<c>item.Text</c>, <c>item.Value</c>, <c>ItemCode</c>, <c>StoreName</c>), which
    ///         <c>System.Text.Json</c>'s default camelCase policy would rename and break.
    ///     </para>
    /// </summary>
    public static class AsmxCompatibility
    {
        /// <summary>
        ///     The former
        ///     <c>&lt;system.web.extensions&gt;...&lt;jsonSerialization maxJsonLength="5000000"&gt;</c>
        ///     ceiling (5 MB). It bounded BOTH the deserialized request and the serialized response
        ///     of a <c>ScriptService</c>, and is applied to both here. It matters for this module in
        ///     particular: the entry services post the whole product grid as the <c>data</c> /
        ///     <c>models</c> argument.
        /// </summary>
        public const int MaxJsonLength = 5000000;

        private static readonly string[] GetAndPost = { "GET", "POST" };

        /// <summary>
        ///     The verbs every former ASMX endpoint answers, matching the old
        ///     <c>&lt;webServices&gt;&lt;protocols&gt;</c> block (HttpGet + HttpPost).
        /// </summary>
        public static string[] Verbs
        {
            get { return GetAndPost; }
        }

        /// <summary>
        ///     Reads the method arguments the javascript caller sent. <c>getAjax</c> posts
        ///     <c>{"name":"value",...}</c> as <c>application/json</c>; a GET caller passes the same
        ///     names on the query string.
        /// </summary>
        public static async Task<AsmxParameters> ReadParametersAsync(HttpContext context)
        {
            JObject body = null;

            if (context != null && context.Request.ContentLength != 0 &&
                !HttpMethods.IsGet(context.Request.Method))
            {
                string json = await ReadBodyAsync(context.Request).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    //A ScriptService answered a malformed body with the same error envelope, which
                    //is what letting a JsonReaderException out of here produces.
                    JToken parsed = JToken.Parse(json);
                    body = parsed as JObject;
                }
            }

            return new AsmxParameters(body, context);
        }

        /// <summary>
        ///     Writes <c>{ "d": &lt;payload&gt; }</c>, the <c>ScriptService</c> response envelope.
        /// </summary>
        public static IResult Envelope(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);

            EnforceJsonLength(json.Length, "response");

            return Results.Text("{\"d\":" + json + "}", "application/json", System.Text.Encoding.UTF8);
        }

        /// <summary>
        ///     The <c>ScriptService</c> error envelope: HTTP 500 plus
        ///     <c>{ "Message", "StackTrace", "ExceptionType" }</c>. The deleted
        ///     <c>Services/web.config</c> had <c>&lt;customErrors mode="Off" /&gt;</c>, so the RAW
        ///     message is surfaced - the entry screens display it verbatim, and the stock/party
        ///     validation messages of these services ARE that message.
        /// </summary>
        public static IResult Error(Exception exception)
        {
            Log.Warning("A Finance service endpoint faulted. {Exception}.", exception);

            string json = JsonConvert.SerializeObject(new
            {
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                ExceptionType = exception.GetType().FullName
            });

            return Results.Text(json, "application/json", System.Text.Encoding.UTF8,
                StatusCodes.Status500InternalServerError);
        }

        private static async Task<string> ReadBodyAsync(HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                EnforceJsonLength(request.ContentLength.Value, "request");
            }

            //Read with the 5 MB ceiling enforced as we go, so an unbounded chunked body cannot be
            //buffered either.
            byte[] buffer = new byte[8192];
            using (MemoryStream target = new MemoryStream())
            {
                int read;

                while ((read = await request.Body.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    target.Write(buffer, 0, read);
                    EnforceJsonLength(target.Length, "request");
                }

                return System.Text.Encoding.UTF8.GetString(target.ToArray());
            }
        }

        private static void EnforceJsonLength(long length, string direction)
        {
            if (length > MaxJsonLength)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Error during serialization or deserialization using the JSON JavaScriptSerializer. The length of the {0} string exceeds the value set on the maxJsonLength property ({1}).",
                    direction, MaxJsonLength));
            }
        }
    }

    /// <summary>
    ///     The arguments of a former <c>[WebMethod]</c>, read from the JSON body (POST) or the query
    ///     string (GET). The conversions reproduce what the <c>JavaScriptSerializer</c> did when it
    ///     bound the posted values onto the method signature.
    /// </summary>
    public sealed class AsmxParameters
    {
        private readonly JObject body;
        private readonly HttpContext context;

        internal AsmxParameters(JObject body, HttpContext context)
        {
            this.body = body;
            this.context = context;
        }

        public string GetString(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type != JTokenType.Null)
            {
                return token.Type == JTokenType.Object || token.Type == JTokenType.Array
                    ? token.ToString(Formatting.None)
                    : token.Value<string>();
            }

            return this.GetQueryValue(name);
        }

        public bool GetBoolean(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type != JTokenType.Null)
            {
                if (token.Type == JTokenType.Boolean)
                {
                    return token.Value<bool>();
                }

                return ParseBoolean(token.Value<string>());
            }

            return ParseBoolean(this.GetQueryValue(name));
        }

        public int GetInt32(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type != JTokenType.Null)
            {
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                {
                    return token.Value<int>();
                }

                return ParseInt32(token.Value<string>());
            }

            return ParseInt32(this.GetQueryValue(name));
        }

        public long GetInt64(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type != JTokenType.Null)
            {
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                {
                    return token.Value<long>();
                }

                return ParseInt64(token.Value<string>());
            }

            return ParseInt64(this.GetQueryValue(name));
        }

        public decimal GetDecimal(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type != JTokenType.Null)
            {
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                {
                    return token.Value<decimal>();
                }

                return ParseDecimal(token.Value<string>());
            }

            return ParseDecimal(this.GetQueryValue(name));
        }

        /// <summary>
        ///     A non-nullable <c>DateTime</c> argument. A <c>JavaScriptSerializer</c> bound a
        ///     missing or unparseable value to <c>default(DateTime)</c>, which is what is returned.
        /// </summary>
        public DateTime GetDateTime(string name)
        {
            DateTime? value = this.GetNullableDateTime(name);

            return value ?? default(DateTime);
        }

        /// <summary>
        ///     A <c>DateTime?</c> argument (the receipt service's <c>postedDate</c>). An absent,
        ///     null or blank value stays null, exactly as the serializer left it - the posting
        ///     procedure distinguishes "no posted date" from a date.
        /// </summary>
        public DateTime? GetNullableDateTime(string name)
        {
            JToken token = this.GetToken(name);

            if (token != null && token.Type == JTokenType.Date)
            {
                return token.Value<DateTime>();
            }

            string text = token != null && token.Type != JTokenType.Null
                ? token.Value<string>()
                : this.GetQueryValue(name);

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            DateTime parsed;

            //The javascript callers send the value formatted with the user's culture (the
            //shortDateFormat global), so the current culture is tried first - as the
            //JavaScriptSerializer's own binding did - with the invariant round-trip form as the
            //fallback for an ISO value.
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            return null;
        }

        /// <summary>
        ///     A COMPLEX-OBJECT array argument, deserialized onto a typed
        ///     <c>Collection&lt;T&gt;</c>. Carried over with the rest of this file from the
        ///     Purchase copy. Finance needs it for <c>CurrencyData/SaveExchangeRates</c>, the
        ///     one former Finance <c>[WebMethod]</c> that took a complex-object array
        ///     (<c>List&lt;Data.Models.ExchangeRate&gt;</c>). Every other Finance web method takes
        ///     primitives and receives its grid as the <c>data</c> JSON STRING, which the ported
        ///     endpoint parses itself exactly as the legacy
        ///     <c>JavaScriptSerializer.Deserialize&lt;dynamic&gt;</c> did (the positional
        ///     <c>item[0..9]</c> access is behaviour, not transport).
        ///     <para>
        ///         A <c>ScriptService</c> bound that array with the <c>JavaScriptSerializer</c>,
        ///         matching JSON member names to CLR property names case-INSENSITIVELY and leaving
        ///         absent members at their default - which is what Newtonsoft does here by default
        ///         too. A missing or null <c>details</c> returns null, so the ported method body's
        ///         own <c>ArgumentNullException</c> guard still fires exactly as before.
        ///     </para>
        /// </summary>
        public Collection<T> GetCollection<T>(string name)
        {
            JToken token = this.GetToken(name);

            if (token == null || token.Type == JTokenType.Null)
            {
                //A GET caller (or a form post) sends the array as a JSON string.
                string text = this.GetQueryValue(name);

                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                token = JToken.Parse(text);
            }

            if (token.Type == JTokenType.String)
            {
                string text = token.Value<string>();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                token = JToken.Parse(text);
            }

            if (token.Type != JTokenType.Array)
            {
                return null;
            }

            Collection<T> collection = new Collection<T>();

            foreach (JToken item in (JArray)token)
            {
                collection.Add(item.ToObject<T>());
            }

            return collection;
        }

        private JToken GetToken(string name)
        {
            if (this.body == null)
            {
                return null;
            }

            //The javascript callers send the exact parameter names; be case-insensitive anyway so a
            //hand-built GET/POST caller cannot fail silently with a default value.
            JToken token;

            if (this.body.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token))
            {
                return token;
            }

            return null;
        }

        private string GetQueryValue(string name)
        {
            if (this.context == null)
            {
                return null;
            }

            foreach (var pair in this.context.Request.Query)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value.ToString();
                }
            }

            return null;
        }

        private static bool ParseBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            bool result;

            if (bool.TryParse(value, out result))
            {
                return result;
            }

            //"1" / "0", which is what a query-string caller of the old HttpGet protocol sent.
            return value.Trim().Equals("1", StringComparison.Ordinal);
        }

        private static int ParseInt32(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            int result;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private static long ParseInt64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            long result;

            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            decimal result;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0;
        }
    }
}
