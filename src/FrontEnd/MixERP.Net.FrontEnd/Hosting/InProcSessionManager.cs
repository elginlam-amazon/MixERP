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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
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

namespace MixERP.Net.FrontEnd.Hosting
{
    /// <summary>
    ///     In-process session state for System.Web.HttpContext.Session (equivalent of sessionState mode="InProc"
    ///     cookieless="UseCookies" timeout="60"). Session items are kept as live objects in memory, so no serialization
    ///     is required, exactly like the former in-process session store.
    /// </summary>
    internal sealed class InProcSessionManager : ISessionManager
    {
        internal const string CookieName = "ASP.NET_SessionId";
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(60);
        private readonly IMemoryCache cache;

        public InProcSessionManager(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public Task<ISessionState> CreateAsync(HttpContext context, SessionAttribute metadata)
        {
            string sessionId = context.Request.Cookies[CookieName];
            ConcurrentDictionary<string, object> items = null;
            bool isNew = false;

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                items = this.cache.Get<ConcurrentDictionary<string, object>>(GetKey(sessionId));
            }

            if (items == null)
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sessionId = CreateSessionId();
                    context.Response.Cookies.Append(CookieName, sessionId, new CookieOptions
                    {
                        HttpOnly = true,
                        IsEssential = true,
                        Path = "/",
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps
                    });
                }

                items = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                isNew = true;
            }

            this.cache.Set(GetKey(sessionId), items, new MemoryCacheEntryOptions {SlidingExpiration = SessionTimeout});

            ISessionState state = new InProcSessionState(sessionId, items, isNew, metadata != null && metadata.IsReadOnly,
                (int)SessionTimeout.TotalMinutes, id => this.cache.Remove(GetKey(id)));

            return Task.FromResult(state);
        }

        private static string GetKey(string sessionId)
        {
            return "MixERP.Session." + sessionId;
        }

        private static string CreateSessionId()
        {
            byte[] bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private sealed class InProcSessionState : ISessionState
        {
            private readonly ConcurrentDictionary<string, object> items;
            private readonly Action<string> remove;

            public InProcSessionState(string sessionId, ConcurrentDictionary<string, object> items, bool isNewSession, bool isReadOnly, int timeout,
                Action<string> remove)
            {
                this.SessionID = sessionId;
                this.items = items;
                this.IsNewSession = isNewSession;
                this.IsReadOnly = isReadOnly;
                this.Timeout = timeout;
                this.remove = remove;
            }

            public string SessionID { get; }
            public bool IsReadOnly { get; }
            public int Timeout { get; set; }
            public bool IsNewSession { get; }
            public int Count => this.items.Count;
            public bool IsSynchronized => true;
            public object SyncRoot => this.items;
            public bool IsAbandoned { get; set; }
            public IEnumerable<string> Keys => this.items.Keys;

            public object this[string key]
            {
                get { return this.items.TryGetValue(key, out object value) ? value : null; }
                set
                {
                    if (value == null)
                    {
                        this.items.TryRemove(key, out object unused);
                        return;
                    }

                    this.items[key] = value;
                }
            }

            public void Remove(string key)
            {
                this.items.TryRemove(key, out object unused);
            }

            public void Clear()
            {
                this.items.Clear();
            }

            public Task CommitAsync(CancellationToken token)
            {
                if (this.IsAbandoned)
                {
                    this.items.Clear();
                    this.remove(this.SessionID);
                }

                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
