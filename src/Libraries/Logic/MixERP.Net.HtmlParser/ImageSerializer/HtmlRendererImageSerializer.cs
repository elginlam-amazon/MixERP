using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    /// Renders HTML to a raster image so it can be attached to an outgoing email.
    ///
    /// MIGRATION NOTE (.NET Framework -> .NET 10, see KB doc 21):
    /// The original implementation used TheArtOfDev.HtmlRenderer.WinForms, which
    /// depends on System.Windows.Forms and only runs on Windows. There is no
    /// drop-in, Linux-safe replacement that performs full CSS/HTML layout and
    /// rasterization without adding a new rendering dependency (e.g. a headless
    /// Chromium via PuppeteerSharp, or wkhtmltoimage). Introducing such a
    /// dependency is a product/architecture decision, not a mechanical port, so
    /// it is intentionally NOT done here. See summary.md "Next steps".
    ///
    /// This implementation fails fast with a clear, actionable exception rather
    /// than silently producing a blank/incorrect image.
    /// </summary>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            throw new PlatformNotSupportedException(
                "HTML-to-image rendering (HtmlRendererImageSerializer) relied on " +
                "TheArtOfDev.HtmlRenderer.WinForms, which is Windows-only and is not " +
                "available on .NET 10 / Linux. Replace this with a cross-platform HTML " +
                "rendering solution (e.g. headless Chromium via PuppeteerSharp) before " +
                "using this feature in production. See migration knowledge base doc 21 " +
                "(System.Drawing.Common Migration) and summary.md Next Steps.");
        }
    }
}