using System;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public interface IHtmlImageSerializer
    {
        string TempDirectory { get; set; }

        string Html { get; set; }

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void Serialize();
    }
}