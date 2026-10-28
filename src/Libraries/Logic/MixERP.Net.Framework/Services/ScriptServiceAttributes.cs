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

namespace MixERP.Net.Framework.Services
{
    /// <summary>Marks a <see cref="WebService"/> whose web methods can be called from script with JSON (formerly System.Web.Script.Services.ScriptServiceAttribute).</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ScriptServiceAttribute : Attribute
    {
    }

    /// <summary>Exposes a public method of a <see cref="WebService"/> (formerly System.Web.Services.WebMethodAttribute).</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        public bool EnableSession { get; set; }
    }

    /// <summary>Response format of a script method (formerly System.Web.Script.Services.ResponseFormat).</summary>
    public enum ResponseFormat
    {
        Json = 0,
        Xml = 1
    }

    /// <summary>Controls how a web method is invoked from script (formerly System.Web.Script.Services.ScriptMethodAttribute).</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptMethodAttribute : Attribute
    {
        public ResponseFormat ResponseFormat { get; set; }
        public bool UseHttpGet { get; set; }
    }
}
