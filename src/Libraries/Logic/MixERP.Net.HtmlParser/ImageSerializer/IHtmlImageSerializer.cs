using SixLabors.ImageSharp.Formats;
using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    // Exposes SixLabors.ImageSharp's IImageFormat, which is not CLS-compliant.
    [CLSCompliant(false)]
    public interface IHtmlImageSerializer
    {
        string TempDirectory { get; set; }

        string Html { get; set; }

        IImageFormat ImageFormat { get; set; }

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void Serialize();
    }
}
