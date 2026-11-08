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
using System.Data;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace MixERP.Net.Common.Helpers
{
    public static class ExpressionHelper
    {
        //System.CodeDom runtime compilation (CSharpCodeProvider/CompilerParameters) is not supported
        //on modern .NET. The expression is now evaluated with Roslyn scripting, which keeps the
        //original semantics: evaluate the expression, return its string form, and return
        //string.Empty when the expression cannot be compiled, throws, or evaluates to null.
        private static readonly ScriptOptions Options = ScriptOptions.Default
            .WithImports("System", "System.Math", "System.Xml", "System.Data")
            .WithReferences(
                typeof(object).Assembly,          //System.Private.CoreLib / System.Runtime
                typeof(Math).Assembly,
                typeof(XmlDocument).Assembly,     //System.Private.Xml
                typeof(DataTable).Assembly);      //System.Data.Common

        public static string Eval(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return string.Empty;
            }

            try
            {
                //The public signature is frozen synchronous, so the async Roslyn API is bridged here.
                object output = CSharpScript.EvaluateAsync<object>(expression, Options).GetAwaiter().GetResult();

                if (output == null)
                {
                    return string.Empty;
                }

                return output.ToString();
            }
            catch (CompilationErrorException)
            {
                //Swallow: the CodeDom version returned string.Empty on compiler errors.
            }
            catch
            {
                //Swallow
            }

            return string.Empty;
        }
    }
}
