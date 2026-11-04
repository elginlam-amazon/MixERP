using System;
using SkiaSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <remarks>
    ///     SkiaSharp replaced System.Drawing here (knowledge base doc 21) and is not marked CLS
    ///     compliant, so <see cref="ImageFormat" /> is not either. A CLS compliant interface may not
    ///     carry a non compliant member (CS3010), so the whole interface is declared non compliant.
    /// </remarks>
    [CLSCompliant(false)]
    public interface IHtmlImageSerializer
    {
        string TempDirectory { get; set; }

        string Html { get; set; }

        SKEncodedImageFormat ImageFormat { get; set; }

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void Serialize();
    }
}