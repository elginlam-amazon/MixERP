using System;
using SkiaSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public interface IHtmlImageSerializer
    {
        string TempDirectory { get; set; }

        string Html { get; set; }

        SKEncodedImageFormat ImageFormat { get; set; }

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void Serialize();
    }
}