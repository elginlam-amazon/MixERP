/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using SkiaSharp;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    /// Image helpers. System.Drawing (GDI+) is Windows-only on .NET 10, so images are processed with SkiaSharp.
    /// </summary>
    public static class ImageHelper
    {
        // Image.Save(stream, ImageFormat.Jpeg) used the GDI+ default JPEG quality of 75.
        private const int JpegQuality = 75;

        public static string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".bmp": return "Image/bmp";
                case ".gif": return "Image/gif";
                case ".jpg": return "Image/jpeg";
                case ".jpeg": return "Image/jpeg";
                case ".png": return "Image/png";
                default: return "text/plain";
            }
        }

        /// <summary>
        /// Returns true when the data can be decoded as an image.
        /// </summary>
        public static bool IsValidImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return false;
            }

            using (SKCodec codec = SKCodec.Create(new SKMemoryStream(imageData)))
            {
                return codec != null;
            }
        }

        /// <summary>
        /// Scales the image to fit a width x height thumbnail (0 keeps the original dimension), centered on a transparent
        /// canvas, and returns it as JPEG.
        /// </summary>
        public static byte[] GetResizedImage(byte[] imageData, int width, int height)
        {
            if (imageData == null)
            {
                return null;
            }

            using (SKBitmap image = SKBitmap.Decode(imageData))
            {
                if (image == null)
                {
                    return null;
                }

                SKSizeI imageSize = new SKSizeI(image.Width, image.Height);
                SKSizeI thumbnailSize = new SKSizeI(width, height);

                if (thumbnailSize.Width.Equals(0))
                {
                    thumbnailSize.Width = imageSize.Width;
                }

                if (thumbnailSize.Height.Equals(0))
                {
                    thumbnailSize.Height = imageSize.Height;
                }

                float scalingRatio = CalculateScalingRatio(imageSize, thumbnailSize);

                int scaledWidth = (int)Math.Round(imageSize.Width * scalingRatio);
                int scaledHeight = (int)Math.Round(imageSize.Height * scalingRatio);
                int scaledLeft = (thumbnailSize.Width - scaledWidth) / 2;
                int scaledTop = (thumbnailSize.Height - scaledHeight) / 2;

                // For portrait mode, adjust the vertical top of the crop area so that we get more of
                // the top area
                if (scaledWidth < scaledHeight && scaledHeight > thumbnailSize.Height)
                {
                    scaledTop = (thumbnailSize.Height - scaledHeight) / 4;
                }

                SKRect cropArea = SKRect.Create(scaledLeft, scaledTop, scaledWidth, scaledHeight);

                using (SKBitmap thumbnail = new SKBitmap(new SKImageInfo(thumbnailSize.Width, thumbnailSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul)))
                using (SKCanvas canvas = new SKCanvas(thumbnail))
                using (SKImage source = SKImage.FromBitmap(image))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawImage(source, cropArea, new SKSamplingOptions(SKCubicResampler.Mitchell));
                    canvas.Flush();

                    using (SKImage result = SKImage.FromBitmap(thumbnail))
                    using (SKData data = result.Encode(SKEncodedImageFormat.Jpeg, JpegQuality))
                    {
                        return data.ToArray();
                    }
                }
            }
        }

        private static float CalculateScalingRatio(SKSizeI originalSize, SKSizeI targetSize)
        {
            float originalAspectRatio = (float)originalSize.Width / originalSize.Height;
            float targetAspectRatio = (float)targetSize.Width / targetSize.Height;

            float scalingRatio;

            if (targetAspectRatio >= originalAspectRatio)
            {
                scalingRatio = (float)targetSize.Width / originalSize.Width;
            }
            else
            {
                scalingRatio = (float)targetSize.Height / originalSize.Height;
            }

            return scalingRatio;
        }
    }
}
