using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            throw new PlatformNotSupportedException("HtmlRenderer-based image serialization is not supported on this platform.");
        }
    }
}
