using System.Security.Principal;
using System.Web;
using System.Web.SessionState;

namespace MixERP.Net.Framework.WebServices
{
    /// <summary>
    ///     Base class for MixERP script (JSON) web services. Replaces System.Web.Services.WebService,
    ///     which does not exist on .NET. Methods decorated with <see cref="WebMethodAttribute" /> are
    ///     exposed by the host application at "~/{path}.asmx/{MethodName}" using the ASP.NET AJAX
    ///     JSON contract (request body is a JSON object of named arguments, response is wrapped in "d").
    /// </summary>
    public abstract class WebService
    {
        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        public HttpSessionState Session
        {
            get { return this.Context == null ? null : this.Context.Session; }
        }

        public HttpServerUtility Server
        {
            get { return this.Context == null ? null : this.Context.Server; }
        }

        public HttpApplicationState Application
        {
            get { return this.Context == null ? null : this.Context.Application; }
        }

        public IPrincipal User
        {
            get { return this.Context == null ? null : this.Context.User; }
        }
    }
}
