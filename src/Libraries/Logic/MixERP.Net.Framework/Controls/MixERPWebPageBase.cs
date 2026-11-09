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

namespace MixERP.Net.Framework.Controls
{
    public abstract class MixERPWebPageBase : System.IDisposable
    {
        protected virtual void InitializeCulture() { }
        protected virtual void OnInit(System.EventArgs e) { }
        protected virtual void OnLoad(System.EventArgs e) { }
        protected virtual void OnPreRender(System.EventArgs e) { }
        public virtual void Dispose() { }

        // WebForms-style page members (stubs for net10 migration)
        public virtual System.Web.HttpRequest Request => System.Web.HttpContext.Current?.Request;
        public virtual System.Web.HttpResponse Response => System.Web.HttpContext.Current?.Response;
        public virtual System.Web.HttpServerUtility Server => System.Web.HttpContext.Current?.Server;
        public virtual dynamic Session => System.Web.HttpContext.Current?.Session;
        public virtual System.Security.Principal.IPrincipal User => System.Web.HttpContext.Current?.User;
        public virtual System.Web.HttpContext Context => System.Web.HttpContext.Current;
        public virtual MixERPWebPageBase Page => this;
        public virtual System.Collections.Generic.List<object> Controls { get; } = new System.Collections.Generic.List<object>();
        public virtual string ID { get; set; }
        public virtual bool IsPostBack => false;
        public virtual string MasterPageFile { get; set; }
        public virtual bool IsAuthenticated => System.Web.HttpContext.Current?.Request?.IsAuthenticated ?? false;
        public virtual string ResolveUrl(string relativeUrl) => relativeUrl;
        public virtual void DataBind() { }
    }
}
