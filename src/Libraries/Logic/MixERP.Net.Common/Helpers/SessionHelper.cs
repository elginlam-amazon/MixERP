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
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;

namespace MixERP.Net.Common.Helpers
{
    public static class SessionHelper
    {
        public static void RemoveSessionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ISession session = GetSession();

            if (session != null && session.Get(key) != null)
            {
                session.Remove(key);
            }
        }

        public static void AddSessionKey(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ISession session = GetSession();

            if (session == null)
            {
                return;
            }

            //ISession stores bytes/strings only, therefore the value is serialized.
            session.SetString(key, Serialize(value));
        }

        public static object GetSessionKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                ISession session = GetSession();

                if (session != null)
                {
                    string value = session.GetString(key);

                    if (value != null)
                    {
                        return Deserialize(value);
                    }
                }
            }

            return null;
        }

        internal static ISession GetSession()
        {
            HttpContext context = MixERPConfiguration.HttpContext;

            if (context == null)
            {
                //There is no ambient HttpContext: background work, or a Blazor Server circuit
                //outside of the initial request. Degrade safely instead of throwing.
                Log.Debug("No ambient HttpContext is available; the session could not be accessed.");
                return null;
            }

            try
            {
                return context.Session;
            }
            catch (InvalidOperationException ex)
            {
                //Session middleware was not configured on the host application.
                Log.Warning(ex, "Session state is not available on the current HttpContext.");
                return null;
            }
        }

        private static string Serialize(object value)
        {
            string text = value as string;

            if (text != null)
            {
                return text;
            }

            return JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        private static object Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            char first = value[0];

            if (first != '{' && first != '[')
            {
                //Stored verbatim as a string.
                return value;
            }

            try
            {
                return JsonConvert.DeserializeObject(value, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                });
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "Could not deserialize the session value.");
                return value;
            }
        }
    }
}
