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

using System.Linq;
using System.Reflection;

namespace MixERP.Net.FrontEnd.Components.Layout
{
    /// <summary>
    ///     The product version literal of the footer / brand menu. Port of the identical
    ///     <c>GetProductVersion()</c> that <c>MixERPMaster.Master.cs</c> and
    ///     <c>BackendMaster.Master.cs</c> each declared (checklist tasks 61 and 62); the two copies
    ///     are folded into one so they cannot drift.
    ///     <para>
    ///         The value comes from the host assembly's
    ///         <see cref="AssemblyInformationalVersionAttribute" />, which is now produced by the
    ///         csproj's <c>&lt;InformationalVersion&gt;</c> (with
    ///         <c>IncludeSourceRevisionInInformationalVersion=false</c>, so no git-SHA suffix is
    ///         appended to the displayed literal).
    ///     </para>
    /// </summary>
    public static class ProductVersion
    {
        /// <summary>
        ///     The informational version of the host assembly. <c>Single()</c> is kept from the
        ///     original: a host built without an informational version is a build error, not something
        ///     to paper over with an empty version in the footer.
        /// </summary>
        public static string GetProductVersion()
        {
            AssemblyInformationalVersionAttribute attribute = (AssemblyInformationalVersionAttribute)
                typeof(ProductVersion).Assembly
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true)
                    .Single();

            return attribute.InformationalVersion;
        }
    }
}
