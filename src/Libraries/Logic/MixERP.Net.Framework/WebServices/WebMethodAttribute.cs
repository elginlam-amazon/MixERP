using System;

namespace MixERP.Net.Framework.WebServices
{
    /// <summary>
    ///     Marks a public method of a <see cref="WebService" /> as callable from the client.
    ///     Replaces System.Web.Services.WebMethodAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        /// <summary>
        ///     Session state is always available to web methods; kept for source compatibility.
        /// </summary>
        public bool EnableSession { get; set; }
    }
}
