using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Rasterizes HTML by navigating the Internet Explorer based WinForms
    ///     <c>System.Windows.Forms.WebBrowser</c> control and capturing it.
    /// </summary>
    /// <remarks>
    ///     WinForms is Windows only and the <c>WebBrowser</c> control is additionally tied to the
    ///     retired Internet Explorer engine, so this has no counterpart on modern .NET. The original
    ///     implementation is kept beside this file as <c>WebBrowserImageSerializer.WinForms.cs</c>
    ///     (excluded from the compile) for the eventual port to a headless browser. Until then
    ///     calling this serializer fails loudly rather than writing an empty image.
    /// </remarks>
    public sealed class WebBrowserImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            throw new PlatformNotSupportedException(
                "Rendering HTML to an image requires the WinForms WebBrowser control, which is " +
                "Windows only and was not ported to modern .NET.");
        }
    }
}
