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
using System.Threading;
using MixERP.Net.Framework;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    internal static class ApplicationError
    {
        internal static void Handle(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            if (ex is ThreadAbortException)
            {
                // ThreadAbortException can no longer be raised on modern .NET
                // (Thread.Abort throws PlatformNotSupportedException), so this branch is
                // unreachable in practice. It is kept because it is the ONLY behaviour the
                // original had for this type: log at Verbose and swallow.
                Log.Verbose("The thread was being aborted. {Exception}.", ex);
                return;
            }


            MixERPException exception = ex as MixERPException;

            if (exception != null)
            {
                Log.Verbose("Handling exception.");

                MixERPException.Handle(exception);
                return;
            }

            Log.Error("Exception occurred. {Exception}.", ex);

            var innerException = ex.InnerException as MixERPException;

            if (innerException != null)
            {
                MixERPException.Handle(innerException);
                return;
            }

            if (ex.InnerException != null)
            {
                Log.Error("Inner Exception. {InnerException}.", ex.InnerException);
            }

            // The original carried a dead `p:` label here (nothing branched to it); it is dropped.
            throw ex;
        }
    }
}
