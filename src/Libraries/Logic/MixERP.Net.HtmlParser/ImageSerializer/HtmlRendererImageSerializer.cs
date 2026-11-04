using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Rasterizes HTML through <c>TheArtOfDev.HtmlRenderer.WinForms</c>.
    /// </summary>
    /// <remarks>
    ///     The renderer is a WinForms/GDI+ component and has no cross platform counterpart, so on
    ///     modern .NET there is no engine available to turn the markup into a bitmap. The original
    ///     implementation is kept beside this file as <c>HtmlRendererImageSerializer.WinForms.cs</c>
    ///     (excluded from the compile) for the eventual port to a headless browser or to
    ///     HtmlRenderer.Core with a SkiaSharp adapter. Until then calling this serializer fails
    ///     loudly rather than writing an empty image.
    /// </remarks>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            throw new PlatformNotSupportedException(
                "Rendering HTML to an image requires TheArtOfDev.HtmlRenderer.WinForms, which is " +
                "Windows only and was not ported to modern .NET.");
        }
    }
}
