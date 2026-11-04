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

namespace MixERP.Net.Common.Helpers
{
    public static class ExpressionHelper
    {
        /// <summary>
        ///     Evaluates a C# expression at run time and returns its result as a string.
        /// </summary>
        /// <remarks>
        ///     The original implementation emitted a throw-away assembly with
        ///     <c>System.CodeDom.Compiler.CSharpCodeProvider</c>, which drove <c>csc.exe</c> and only
        ///     ever worked on .NET Framework. CodeDOM compilation is not part of modern .NET, so the
        ///     equivalent is Roslyn scripting (<c>Microsoft.CodeAnalysis.CSharp.Scripting</c>). The
        ///     member is kept so the public surface of this library is unchanged, and it fails loudly
        ///     instead of silently returning nothing. The CodeDOM original is kept beside this file as
        ///     <c>ExpressionHelper.CodeDom.cs</c> (excluded from the compile) for that port, which is
        ///     tracked in summary.md.
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">Always.</exception>
        public static string Eval(string expression)
        {
            throw new PlatformNotSupportedException(
                "Evaluating a C# expression at run time required the CodeDOM CSharpCodeProvider, " +
                "which is not available on modern .NET. Port this to Roslyn scripting " +
                "(Microsoft.CodeAnalysis.CSharp.Scripting) before using it.");
        }
    }
}
