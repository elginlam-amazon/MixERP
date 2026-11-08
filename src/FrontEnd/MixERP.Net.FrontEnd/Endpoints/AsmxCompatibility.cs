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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     The transport parity layer for the five former <c>Services/*.asmx</c>
    ///     <c>[ScriptService]</c> web services (checklist task 80).
    ///     <para>
    ///         THE ENVELOPE IS PRESERVED. An ASP.NET <c>ScriptService</c> wrapped every return value
    ///         in <c>{ "d": &lt;value&gt; }</c> and every unhandled exception in
    ///         <c>{ "Message": ..., "StackTrace": ..., "ExceptionType": ... }</c> with status 500.
    ///         Every javascript caller in this solution reads <c>msg.d</c> (and
    ///         <c>getAjaxErrorMessage</c> in <c>mixerp-core.js</c> reads
    ///         <c>JSON.parse(xhr.responseText).Message</c>), including the eight <c>Modules/*</c>
    ///         subprojects which are NOT part of this migration pass. Keeping the envelope is
    ///         therefore the only choice that leaves every caller — ported or not — working, so the
    ///         javascript changes of task 81 are URL-only.
    ///     </para>
    ///     <para>
    ///         NEWTONSOFT, NOT System.Text.Json. The payloads are serialized with
    ///         <see cref="JsonConvert" /> and written as raw text: the callers index the CLR property
    ///         names (<c>item.OfficeId</c>, <c>Status</c>, <c>NavigateUrl</c>), which
    ///         <c>System.Text.Json</c>'s default camelCase policy would rename and break.
    ///     </para>
    ///     <para>
    ///         VERBS. The deleted <c>Services/web.config</c> enabled <c>HttpGet</c> AND
    ///         <c>HttpPost</c> under <c>&lt;webServices&gt;&lt;protocols&gt;</c>, so every endpoint is
    ///         mapped for both verbs and <see cref="ReadParametersAsync" /> reads arguments from the
    ///         JSON request body (POST, what <c>getAjax</c> sends) or the query string (GET).
    ///     </para>
    /// </summary>
    public static class AsmxCompatibility
    {
        /// <summary>
        ///     The former
        ///     <c>&lt;system.web.extensions&gt;...&lt;jsonSerialization maxJsonLength="5000000"&gt;</c>
        ///     ceiling (5 MB). It bounded BOTH the deserialized request and the serialized response of
        ///     a <c>ScriptService</c>, and is applied to both here.
        /// </summary>
        public const int MaxJsonLength = 5000000;

        private static readonly string[] GetAndPost = { "GET", "POST" };

        /// <summary>
        ///     The verbs every former ASMX endpoint answers, matching the old
        ///     <c>&lt;protocols&gt;</c> block.
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
                    //A ScriptService answered a malformed body with the same error envelope, which is
                    //what letting a JsonReaderException out of here produces.
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
            //A string payload is serialized AS a JSON string (quoted and escaped), which is what the
            //menu/authenticate services returned: their callers do JSON.parse(msg.d) /
            //msg.d === "OK".
            string json = JsonConvert.SerializeObject(payload);

            EnforceJsonLength(json.Length, "response");

            return Results.Text("{\"d\":" + json + "}", "application/json", System.Text.Encoding.UTF8);
        }

        /// <summary>
        ///     The <c>ScriptService</c> error envelope: HTTP 500 plus
        ///     <c>{ "Message", "StackTrace", "ExceptionType" }</c>. <c>Services/web.config</c> had
        ///     <c>&lt;customErrors mode="Off" /&gt;</c>, so the RAW message is surfaced (the callers
        ///     display it verbatim) instead of a friendly error page. Writing it here also keeps the
        ///     request away from <c>MixERPExceptionHandlingMiddleware</c>'s redirect path.
        /// </summary>
        public static IResult Error(Exception exception)
        {
            Log.Warning("A service endpoint faulted. {Exception}.", exception);

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

            string query = this.GetQueryValue(name);

            return query;
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
    }
}
