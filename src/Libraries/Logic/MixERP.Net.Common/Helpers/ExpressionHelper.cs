using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace MixERP.Net.Common.Helpers
{
    public static class ExpressionHelper
    {
        private static readonly ScriptOptions Options = ScriptOptions.Default
            .AddReferences(typeof(object).Assembly, typeof(System.Data.DataTable).Assembly)
            .AddImports("System", "System.Data", "System.Linq");

        public static string Eval(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return string.Empty;
            }

            try
            {
                object output = CSharpScript.EvaluateAsync<object>(expression, Options).GetAwaiter().GetResult();
                return output?.ToString() ?? string.Empty;
            }
            catch (CompilationErrorException)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }
    }
}
