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
using System.Linq;
using System.Reflection;

namespace MixERP.Net.Web.UI
{
    /// <summary>
    ///     Base class of controls which were previously defined by a markup file (.aspx, .ascx, .master).
    ///     The markup is compiled into <see cref="FrameworkInitialize" /> overrides (the generated designer partials).
    /// </summary>
    public class TemplateControl : Control, INamingContainer
    {
        private bool frameworkInitialized;

        /// <summary>
        ///     Builds the control tree which was formerly declared in markup. Called once, before Init.
        /// </summary>
        protected virtual void FrameworkInitialize()
        {
        }

        /// <summary>
        ///     When true (the Web Forms default, used by every MixERP page and control), methods named Page_Init,
        ///     Page_Load, Page_PreRender and Page_Unload are wired to the corresponding events.
        /// </summary>
        protected virtual bool SupportAutoEvents => true;

        internal void EnsureFrameworkInitialized()
        {
            if (this.frameworkInitialized)
            {
                return;
            }

            this.frameworkInitialized = true;
            this.FrameworkInitialize();

            if (this.SupportAutoEvents)
            {
                this.HookUpAutomaticHandlers();
            }
        }

        private void HookUpAutomaticHandlers()
        {
            this.Hook("Page_Init", h => this.Init += h);
            this.Hook("Page_Load", h => this.Load += h);
            this.Hook("Page_PreRender", h => this.PreRender += h);
            this.Hook("Page_Unload", h => this.Unload += h);

            if (this is Page page)
            {
                this.Hook("Page_PreInit", h => page.PreInit += h);
                this.Hook("Page_InitComplete", h => page.InitComplete += h);
                this.Hook("Page_PreLoad", h => page.PreLoad += h);
                this.Hook("Page_LoadComplete", h => page.LoadComplete += h);
                this.Hook("Page_PreRenderComplete", h => page.PreRenderComplete += h);
            }
        }

        private void Hook(string methodName, Action<EventHandler> subscribe)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type type = this.GetType(); type != null && type != typeof(TemplateControl); type = type.BaseType)
            {
                MethodInfo withArgs = type.GetMethod(methodName, flags | BindingFlags.DeclaredOnly, null, new[] {typeof(object), typeof(EventArgs)}, null);

                if (withArgs != null)
                {
                    subscribe((EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this, withArgs));
                    return;
                }

                MethodInfo withoutArgs = type.GetMethod(methodName, flags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                if (withoutArgs != null)
                {
                    subscribe((sender, e) => withoutArgs.Invoke(this, null));
                    return;
                }
            }
        }

        public virtual string TemplateSourceDirectory
        {
            get
            {
                string path = ControlRegistry.GetVirtualPath(this.GetType());

                if (string.IsNullOrWhiteSpace(path))
                {
                    return "~/";
                }

                int index = path.LastIndexOf('/');
                return index > 0 ? path.Substring(0, index + 1) : "~/";
            }
        }

        public Control LoadControl(string virtualPath)
        {
            Type type = ControlRegistry.Resolve(virtualPath);

            if (type == null)
            {
                throw new InvalidOperationException("No control is registered for the virtual path \"" + virtualPath + "\".");
            }

            return this.LoadControl(type, null);
        }

        public Control LoadControl(Type t, object[] parameters)
        {
            Control control = (Control)Activator.CreateInstance(t, parameters ?? new object[0]);
            (control as TemplateControl)?.EnsureFrameworkInitialized();
            return control;
        }

        internal override void OnAddedToParent()
        {
            this.EnsureFrameworkInitialized();
        }
    }

    public class UserControl : TemplateControl
    {
        [CLSCompliant(false)]
        public System.Web.HttpRequest Request => this.Context.Request;
        [CLSCompliant(false)]
        public System.Web.HttpResponse Response => this.Context.Response;
        [CLSCompliant(false)]
        public System.Web.HttpServerUtility Server => this.Context.Server;
        [CLSCompliant(false)]
        public System.Web.SessionState.HttpSessionState Session => this.Context.Session;
        [CLSCompliant(false)]
        public System.Web.Caching.Cache Cache => this.Context.Cache;
        public bool IsPostBack => this.Page != null && this.Page.IsPostBack;
    }

    public class MasterPage : UserControl
    {
        public virtual string MasterPageFile { get; set; }
        public MasterPage Master { get; internal set; }

        internal Dictionary<string, WebControls.ContentPlaceHolder> GetContentPlaceHolders()
        {
            return this.DescendantsAndSelf().OfType<WebControls.ContentPlaceHolder>()
                .Where(p => !string.IsNullOrEmpty(p.ID))
                .GroupBy(p => p.ID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Maps former markup virtual paths (e.g. ~/Modules/Sales/Setup/BonusSlabs.ascx) to the classes that replace them.
    /// </summary>
    public static class ControlRegistry
    {
        private static readonly ConcurrentDictionary<string, Type> Map = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<Type, string> ReverseMap = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<Assembly, bool> Scanned = new ConcurrentDictionary<Assembly, bool>();

        public static void Register(Assembly assembly)
        {
            if (assembly == null || !Scanned.TryAdd(assembly, true) || assembly.IsDynamic)
            {
                return;
            }

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (Type type in types)
            {
                VirtualPathAttribute attribute = type.GetCustomAttribute<VirtualPathAttribute>(false);

                if (attribute != null)
                {
                    string key = Normalize(attribute.VirtualPath);
                    Map[key] = type;
                    ReverseMap[type] = key;
                }
            }
        }

        /// <summary>
        ///     Registers an explicit mapping (e.g. handlers formerly declared in Web.config).
        /// </summary>
        public static void Register(string virtualPath, Type type)
        {
            if (string.IsNullOrWhiteSpace(virtualPath) || type == null)
            {
                return;
            }

            string key = Normalize(virtualPath);
            Map[key] = type;
            ReverseMap.TryAdd(type, key);
        }

        public static void RegisterLoadedAssemblies()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Register(assembly);
            }
        }

        public static Type Resolve(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                return null;
            }

            string key = Normalize(virtualPath);

            if (Map.TryGetValue(key, out Type type))
            {
                return type;
            }

            RegisterLoadedAssemblies();
            return Map.TryGetValue(key, out type) ? type : null;
        }

        public static bool Exists(string virtualPath)
        {
            return Resolve(virtualPath) != null;
        }

        public static string GetVirtualPath(Type type)
        {
            return type != null && ReverseMap.TryGetValue(type, out string path) ? path : null;
        }

        public static IEnumerable<KeyValuePair<string, Type>> All()
        {
            RegisterLoadedAssemblies();
            return Map.ToArray();
        }

        private static string Normalize(string virtualPath)
        {
            string path = virtualPath.Trim().Replace('\\', '/');

            if (path.StartsWith("~", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            return "~" + path;
        }
    }

    public abstract class PageStatePersister
    {
        protected PageStatePersister(Page page)
        {
            this.Page = page;
        }

        protected Page Page { get; }
        public object ControlState { get; set; }
        public object ViewState { get; set; }
        public abstract void Load();
        public abstract void Save();
    }

    public sealed class RouteData
    {
        public RouteData()
        {
            this.Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, object> Values { get; }
    }
}
