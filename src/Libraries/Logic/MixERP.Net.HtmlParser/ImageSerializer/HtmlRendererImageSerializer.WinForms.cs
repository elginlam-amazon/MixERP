// WinForms implementation retained for reference. HTML to image rasterization needs a Windows
// browser engine: TheArtOfDev.HtmlRenderer.WinForms and System.Windows.Forms.WebBrowser have no
// cross platform equivalent, so this file is excluded from the net10.0 compile and the shipped
// serializer throws PlatformNotSupportedException. See knowledge base doc 21 and summary.md.

using MixERP.Net.Common.Helpers;
using System.Drawing;
using System.Threading;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            Thread thread = new Thread(delegate()
            {
                string path = imagePath + ImageHelper.GetFileExtension(this.ImageFormat);

                Image image = HtmlRender.RenderToImage(this.Html);

                image.Save(path, ImageHelper.GetEncoder(this.ImageFormat), null);
                OnImageSaved(new ImageSavedEventArgs(path));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            base.CreateImage(imagePath);
        }
    }
}