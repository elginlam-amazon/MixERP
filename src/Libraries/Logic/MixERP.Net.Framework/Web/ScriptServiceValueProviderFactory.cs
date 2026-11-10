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
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace MixERP.Net.Framework.Web
{
    /// <summary>
    ///     Exposes the properties of a JSON request body (<c>{"catalog": "x", "items": [...]}</c>)
    ///     as named values, the way ASP.NET AJAX script services bound web method parameters.
    ///     Nested objects and arrays are flattened to <c>name.child</c> / <c>name[0]</c> keys so
    ///     the standard model binders build complex parameters as well.
    /// </summary>
    public sealed class ScriptServiceValueProviderFactory : IValueProviderFactory
    {
        public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            HttpRequest request = context.ActionContext.HttpContext.Request;

            if (!HttpMethods.IsPost(request.Method) || request.ContentType == null ||
                !request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.ActionContext.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor ||
                !typeof(ScriptServiceController).IsAssignableFrom(descriptor.ControllerTypeInfo))
            {
                return;
            }

            request.EnableBuffering();

            // JSON strings (dates, user-entered values) are converted with the request culture,
            // as the script services did; JSON numbers and booleans use the invariant format.
            Dictionary<string, StringValues> values = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, StringValues> literals = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (JsonDocument document = await JsonDocument.ParseAsync(request.Body).ConfigureAwait(false))
                {
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in document.RootElement.EnumerateObject())
                        {
                            Flatten(property.Name, property.Value, values, literals);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Not a JSON object: nothing to bind, exactly like an empty argument list.
            }
            finally
            {
                request.Body.Position = 0;
            }

            context.ValueProviders.Insert(0, new QueryStringValueProvider(BindingSource.Body,
                new QueryCollection(literals), CultureInfo.InvariantCulture));
            context.ValueProviders.Insert(0, new QueryStringValueProvider(BindingSource.Body,
                new QueryCollection(values), CultureInfo.CurrentCulture));
        }

        private static void Flatten(string prefix, JsonElement element, Dictionary<string, StringValues> values,
            Dictionary<string, StringValues> literals)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        Flatten(prefix + "." + property.Name, property.Value, values, literals);
                    }

                    break;
                case JsonValueKind.Array:
                    int index = 0;

                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        Flatten(prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", item, values, literals);
                        index++;
                    }

                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    break;
                case JsonValueKind.String:
                    values[prefix] = element.GetString();
                    break;
                default:
                    literals[prefix] = element.GetRawText();
                    break;
            }
        }
    }
}
