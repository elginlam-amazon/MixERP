using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MixERP.Net.Framework.Web;

namespace MixERP.Net.i18n
{
    public class ResourcesHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string culture = context.Request.QueryString["culture"];
            string script = this.GetScript(culture);

            context.Response.ContentType = "text/javascript";

            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.Headers["Cache-Control"] = "public,max-age=86400";
            context.Response.Headers["Vary"] = "Accept-Encoding";

            context.Response.Write(script);
            context.Response.End();
        }

        private string GetScript(string culture)
        {
            StringBuilder script = new StringBuilder();
            script.Append("var Resources = {");

            IEnumerable<LocalizedResource> resources = DbResources.GetLocalizationTable(culture);

            var resourceClassGroup = resources.GroupBy(r => r.ResourceClass)
                                .Select(group => group.ToList())
                                .ToList();

            foreach (var resourceClass in resourceClassGroup)
            {
                int i = 0;

                foreach (var resource in resourceClass)
                {
                    if (i == 0)
                    {
                        script.Append(resource.ResourceClass + ": {");

                    }

                    script.Append(resource.Key + ": function(){ return \"");
                    string localized = resource.Translated;

                    if (string.IsNullOrWhiteSpace(localized))
                    {
                        localized = resource.Original;
                    }

                    script.Append(System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(localized));

                    script.Append("\";");
                    script.Append("},");
                    i++;
                }

                script.Append("},");
            }


            script.Append("};");

            return script.ToString();
        }
        #endregion
    }
}
