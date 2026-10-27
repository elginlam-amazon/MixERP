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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.SystemWebAdapters.SessionState.Serialization;
using MixERP.Net.Framework;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     ASP.NET Core session state is serialized (the legacy InProc session stored live objects), so every
    ///     session key used by MixERP must be registered with its type.
    /// </summary>
    internal static class SessionKeys
    {
        internal const string Exception = "ex";

        internal static void Register(JsonSessionSerializerOptions options, IEnumerable<Assembly> assemblies)
        {
            options.RegisterKey<string>("Challenge");
            options.RegisterKey<bool>("FirstStepsPending");
            options.RegisterKey<int>("InvalidPasswordAttempts");

            // Stock transaction merge model (MixERP.Net.WebControls.StockTransactionViewFactory.Data), stored by the
            // StockTransactionView control before redirecting to the target entry page.
            Type mergeModel = ModuleLoader.FindType(assemblies, "MixERP.Net.WebControls.StockTransactionViewFactory.Data.Models.MergeModel");

            if (mergeModel != null)
            {
                options.KnownKeys["Product"] = mergeModel;
            }
        }
    }

    /// <summary>
    ///     Stores the exception shown by ~/Site/RuntimeError.aspx (session key "ex"). Exceptions are not JSON
    ///     serializable, so only the message (the only information displayed) is persisted.
    /// </summary>
    internal sealed class ExceptionSessionKeySerializer : ISessionKeySerializer
    {
        public bool TrySerialize(string key, object value, out byte[] bytes)
        {
            Exception exception = value as Exception;

            if (key == SessionKeys.Exception && exception != null)
            {
                bytes = Encoding.UTF8.GetBytes(exception.Message ?? string.Empty);
                return true;
            }

            bytes = null;
            return false;
        }

        public bool TryDeserialize(string key, byte[] bytes, out object obj)
        {
            if (key == SessionKeys.Exception && bytes != null)
            {
                obj = new MixERPException(Encoding.UTF8.GetString(bytes));
                return true;
            }

            obj = null;
            return false;
        }
    }
}
