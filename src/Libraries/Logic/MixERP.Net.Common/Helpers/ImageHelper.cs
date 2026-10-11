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
using System.IO;
using SkiaSharp;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Image helpers. Uses SkiaSharp (cross-platform) instead of System.Drawing, which is Windows-only on modern .NET.
    /// </summary>
    public static class ImageHelper
    {
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

        [CLSCompliant(false)]
        public static string GetFileExtension(SKEncodedImageFormat format)
        {
            switch (format)
            {
                case SKEncodedImageFormat.Bmp: return ".BMP";
                case SKEncodedImageFormat.Gif: return ".GIF";
                case SKEncodedImageFormat.Png: return ".PNG";
                case SKEncodedImageFormat.Webp: return ".WEBP";
                default: return ".JPG";
            }
        }

        [CLSCompliant(false)]
        public static SKEncodedImageFormat GetImageFormat(String path)
        {
            switch (Path.GetExtension(path))
            {
                case ".bmp": return SKEncodedImageFormat.Bmp;
                case ".gif": return SKEncodedImageFormat.Gif;
                case ".jpg": return SKEncodedImageFormat.Jpeg;
                case ".png": return SKEncodedImageFormat.Png;
            }
            return SKEncodedImageFormat.Jpeg;
        }

        [CLSCompliant(false)]
        public static byte[] GetResizedImage(SKBitmap image, int width, int height)
        {
            if (image == null)
            {
                return null;
            }

            int thumbnailWidth = width.Equals(0) ? image.Width : width;
            int thumbnailHeight = height.Equals(0) ? image.Height : height;

            float scalingRatio = CalculateScalingRatio(image.Width, image.Height, thumbnailWidth, thumbnailHeight);

            int scaledWidth = (int)Math.Round(image.Width * scalingRatio);
            int scaledHeight = (int)Math.Round(image.Height * scalingRatio);
            int scaledLeft = (thumbnailWidth - scaledWidth) / 2;
            int scaledTop = (thumbnailHeight - scaledHeight) / 2;

            // For portrait mode, adjust the vertical top of the crop area so that we get more of
            // the top area
            if (scaledWidth < scaledHeight && scaledHeight > thumbnailHeight)
            {
                scaledTop = (thumbnailHeight - scaledHeight) / 4;
            }

            SKRect cropArea = SKRect.Create(scaledLeft, scaledTop, scaledWidth, scaledHeight);

            using (SKBitmap thumbnail = new SKBitmap(new SKImageInfo(thumbnailWidth, thumbnailHeight, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                using (SKCanvas canvas = new SKCanvas(thumbnail))
                {
                    canvas.Clear(SKColors.Transparent);

                    using (SKImage source = SKImage.FromBitmap(image))
                    {
                        canvas.DrawImage(source, cropArea, new SKSamplingOptions(SKCubicResampler.Mitchell));
                    }
                }

                return Conversion.TryCastByteArray(thumbnail);
            }
        }

        private static float CalculateScalingRatio(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            float originalAspectRatio = (float)originalWidth / originalHeight;
            float targetAspectRatio = (float)targetWidth / targetHeight;

            float scalingRatio;

            if (targetAspectRatio >= originalAspectRatio)
            {
                scalingRatio = (float)targetWidth / originalWidth;
            }
            else
            {
                scalingRatio = (float)targetHeight / originalHeight;
            }

            return scalingRatio;
        }
    }
}
