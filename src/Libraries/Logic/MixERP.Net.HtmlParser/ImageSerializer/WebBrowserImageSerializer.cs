using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class WebBrowserImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            throw new PlatformNotSupportedException("WebBrowser-based image serialization is not supported on this platform.");
        }
    }
}
