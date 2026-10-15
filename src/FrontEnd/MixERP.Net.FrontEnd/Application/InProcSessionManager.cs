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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.AspNetCore.SystemWebAdapters.SessionState;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MixERP.Net.FrontEnd.Application
{
    public sealed class InProcSessionOptions
    {
        public string CookieName { get; set; } = "ASP.NET_SessionId";

        /// <summary>
        ///     Session timeout in minutes (was &lt;sessionState timeout="60" /&gt;).
        /// </summary>
        public int TimeoutMinutes { get; set; } = 60;
    }

    /// <summary>
    ///     In-process System.Web session state (equivalent of &lt;sessionState mode="InProc" /&gt;).
    ///     Session items are kept as live objects in server memory, so MixERP can keep storing
    ///     non-serializable objects (models, collections, exceptions) in session exactly as before.
    /// </summary>
    public sealed class InProcSessionManager : ISessionManager
    {
        private readonly IMemoryCache cache;
        private readonly InProcSessionOptions options;

        public InProcSessionManager(IMemoryCache cache, IOptions<InProcSessionOptions> options)
        {
            this.cache = cache;
            this.options = options.Value;
        }

        public Task<ISessionState> CreateAsync(HttpContext context, SessionAttribute metadata)
        {
            bool isNew = false;
            string sessionId = context.Request.Cookies[this.options.CookieName];
            ConcurrentDictionary<string, object> items = null;

            if (string.IsNullOrWhiteSpace(sessionId) || !this.cache.TryGetValue(GetCacheKey(sessionId), out items))
            {
                isNew = true;
                sessionId = CreateSessionId();
                items = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                context.Response.Cookies.Append(this.options.CookieName, sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps,
                    Path = "/"
                });
            }

            InProcSessionState state = new InProcSessionState(this, sessionId, items, isNew,
                metadata != null && metadata.IsReadOnly, this.options.TimeoutMinutes);

            this.Store(state);

            return Task.FromResult<ISessionState>(state);
        }

        private void Store(InProcSessionState state)
        {
            if (state.IsAbandoned)
            {
                this.cache.Remove(GetCacheKey(state.SessionID));
                return;
            }

            this.cache.Set(GetCacheKey(state.SessionID), state.Items, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(state.Timeout)
            });
        }

        private static string GetCacheKey(string sessionId)
        {
            return "MixERP.Session." + sessionId;
        }

        private static string CreateSessionId()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        }

        private sealed class InProcSessionState : ISessionState
        {
            private readonly InProcSessionManager manager;

            public InProcSessionState(InProcSessionManager manager, string sessionId,
                ConcurrentDictionary<string, object> items, bool isNewSession, bool isReadOnly, int timeout)
            {
                this.manager = manager;
                this.SessionID = sessionId;
                this.Items = items;
                this.IsNewSession = isNewSession;
                this.IsReadOnly = isReadOnly;
                this.Timeout = timeout;
            }

            public ConcurrentDictionary<string, object> Items { get; }

            public string SessionID { get; }
            public bool IsReadOnly { get; }
            public int Timeout { get; set; }
            public bool IsNewSession { get; }
            public bool IsAbandoned { get; set; }

            public int Count
            {
                get { return this.Items.Count; }
            }

            public bool IsSynchronized
            {
                get { return true; }
            }

            public object SyncRoot
            {
                get { return this.Items; }
            }

            public object this[string key]
            {
                get
                {
                    object value;
                    return this.Items.TryGetValue(key, out value) ? value : null;
                }
                set { this.Items[key] = value; }
            }

            public IEnumerable<string> Keys
            {
                get { return this.Items.Keys; }
            }

            public void Remove(string key)
            {
                object removed;
                this.Items.TryRemove(key, out removed);
            }

            public void Clear()
            {
                this.Items.Clear();
            }

            public Task CommitAsync(CancellationToken token)
            {
                this.manager.Store(this);
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }
    }
}
