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

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace MixERP.Net.Framework.Web
{
    /// <summary>
    ///     Base class for the controllers that replace the ASP.NET AJAX script services
    ///     (<c>[ScriptService]</c> <c>.asmx</c> web services). It keeps their wire contract so
    ///     the existing JavaScript keeps working:
    ///     <list type="bullet">
    ///         <item>parameters are read by name from the JSON request body (see
    ///         <see cref="ScriptServiceValueProviderFactory" />);</item>
    ///         <item>results are returned as <c>{"d": result}</c>, serialized like
    ///         JavaScriptSerializer did (PascalCase names, <c>\/Date(ms)\/</c> dates);</item>
    ///         <item>an exception becomes HTTP 500 with <c>{"Message", "ExceptionType"}</c>.</item>
    ///     </list>
    ///     Routes keep the old <c>/path/Service.asmx/Method</c> urls.
    /// </summary>
    public abstract class ScriptServiceController : Controller
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Culture = CultureInfo.InvariantCulture
        };

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Exception != null && !context.ExceptionHandled)
            {
                Exception exception = context.Exception;

                if (exception is AggregateException && exception.InnerException != null)
                {
                    exception = exception.InnerException;
                }

                context.Result = ToJson(new
                {
                    Message = exception.Message,
                    ExceptionType = exception.GetType().FullName
                }, 500);

                context.ExceptionHandled = true;
                return;
            }

            if (context.Result is ObjectResult objectResult)
            {
                context.Result = ToJson(new { d = objectResult.Value }, objectResult.StatusCode ?? 200);
            }
            else if (context.Result is EmptyResult || context.Result == null)
            {
                context.Result = ToJson(new { d = (object)null }, 200);
            }

            base.OnActionExecuted(context);
        }

        private static ContentResult ToJson(object value, int statusCode)
        {
            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(value, SerializerSettings),
                ContentType = "application/json; charset=utf-8",
                StatusCode = statusCode
            };
        }
    }
}
