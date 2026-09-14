// HtmlRendererImageSerializer - stubbed out for .NET 10 Linux compatibility
// TheArtOfDev.HtmlRenderer.WinForms is Windows-only and not available in .NET 10 on Linux

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            // HtmlRenderer.WinForms is not available on Linux/.NET 10
            // This implementation is a no-op stub
            base.CreateImage(imagePath);
        }
    }
}
