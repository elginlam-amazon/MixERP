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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using MixERP.Net.Common;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     The sign-in session surface: the invalid-attempt lockout and the challenge handshake
    ///     (checklist tasks 59 and 60).
    ///     <para>
    ///         Both pieces of state lived in WebForms <c>Session</c> and now live in
    ///         <see cref="ISession" />. <c>PageUtility.InvalidPasswordAttempts(ISession, int)</c> and
    ///         <c>PageUtility.CheckInvalidAttempts(NavigationManager)</c> already carry the counter and
    ///         the <c>MaxInvalidPasswordAttempts</c> lockout in <c>MixERP.Net.Common</c>; this class is
    ///         the host-side wrapper that owns the escalating delay and the challenge key, so no caller
    ///         has to know the session key names.
    ///     </para>
    /// </summary>
    public static class SignInSession
    {
        /// <summary>
        ///     The legacy session key. The value is read by <c>User.Authenticate</c> and written by the
        ///     SignIn page, so the key name is a contract between the two.
        /// </summary>
        public const string ChallengeSessionKey = "Challenge";

        /// <summary>
        ///     Port of the <c>GetDelay()</c> of <c>Services/User.asmx</c> (checklist task 59): the
        ///     escalating delay applied BEFORE every authentication attempt —
        ///     <c>attempts &gt; 3 ? attempts * 10000 : 1000</c> milliseconds.
        /// </summary>
        public static int GetDelay(ISession session)
        {
            int attempts = PageUtility.InvalidPasswordAttempts(session);

            if (attempts > 3)
            {
                return attempts * 10000;
            }

            return 1000;
        }

        /// <summary>
        ///     Port of <c>LogInvalidSignIn()</c>: increments the session counter (which also emits the
        ///     "Invalid attempt to sign in from ..." warning inside
        ///     <c>PageUtility.InvalidPasswordAttempts</c>).
        /// </summary>
        public static void LogInvalidSignIn(ISession session)
        {
            PageUtility.InvalidPasswordAttempts(session, 1);
        }

        /// <summary>
        ///     Port of <c>PageUtility.CheckInvalidAttempts(this.Page)</c> as the SignIn page called it
        ///     (checklist task 59): when the session counter has reached
        ///     <c>MaxInvalidPasswordAttempts</c> the caller is sent to the static
        ///     Access-Is-Denied page. The counter itself is session state, the ceiling is configuration.
        /// </summary>
        public static void CheckInvalidAttempts(NavigationManager navigationManager)
        {
            PageUtility.CheckInvalidAttempts(navigationManager);
        }

        /// <summary>
        ///     Port of the SignIn page's challenge generation (checklist task 60): a dash-stripped GUID
        ///     written to session and emitted to the browser as the <c>challenge</c> javascript global.
        ///     The value is returned so the page can emit it.
        ///     <para>
        ///         BLAZOR SERVER SEAM: session is backed by a cookie-identified store that is only
        ///         writable while an HTTP response is open, so this must be called during the SignIn
        ///         page's INITIAL (static) render — not from an interactive handler. The SignIn page is
        ///         rendered statically for exactly this reason.
        ///     </para>
        /// </summary>
        public static string GenerateChallenge(ISession session)
        {
            string challenge = Guid.NewGuid().ToString().Replace("-", "");

            if (session != null)
            {
                session.SetString(ChallengeSessionKey, challenge);
            }

            return challenge;
        }

        /// <summary>
        ///     The challenge currently held in session, or an empty string.
        ///     <c>User.Authenticate</c> REJECTS the attempt when this is empty — i.e. when the caller
        ///     never loaded the SignIn page in this session.
        /// </summary>
        public static string GetChallenge(ISession session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            return Conversion.TryCastString(session.GetString(ChallengeSessionKey));
        }
    }
}
