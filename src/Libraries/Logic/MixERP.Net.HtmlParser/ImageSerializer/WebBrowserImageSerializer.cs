// WebBrowserImageSerializer - stubbed out for .NET 10 Linux compatibility
// System.Windows.Forms.WebBrowser is Windows-only and not available in .NET 10 on Linux

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class WebBrowserImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            // WebBrowser is not available on Linux/.NET 10
            // This implementation is a no-op stub
            base.CreateImage(imagePath);
        }
    }
}
