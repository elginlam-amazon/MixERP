using MixERP.Net.Common.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + ImageHelper.GetFileExtension(this.ImageFormat);

            //Was TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(this.Html) on an STA
            //thread; headless Chromium renders the same HTML string cross-platform. The STA
            //apartment and Thread.Join are gone, but the call still completes synchronously so
            //ImageSaved is raised before Serialize() returns.
            byte[] rendered = HtmlRenderingEngine.RenderHtml(this.Html, HtmlRenderingEngine.ViewportWidth, 1);

            using (Image image = Image.Load(rendered))
            {
                IImageEncoder encoder = ImageHelper.GetEncoder(this.ImageFormat) ?? new PngEncoder();
                image.Save(path, encoder);
            }

            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
