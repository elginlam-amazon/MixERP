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

using SkiaSharp;
using System;
using System.IO;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    /// Cross-platform (Linux-safe) image helper backed by SkiaSharp.
    /// Replaces the former System.Drawing.Common (GDI+, Windows-only) implementation
    /// as part of the .NET Framework -> .NET 10 migration (see KB doc 21).
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

        public static SKEncodedImageFormat GetEncoder(SKEncodedImageFormat format)
        {
            return format;
        }

        public static string GetFileExtension(SKEncodedImageFormat format)
        {
            switch (format)
            {
                case SKEncodedImageFormat.Bmp: return "BMP";
                case SKEncodedImageFormat.Gif: return "GIF";
                case SKEncodedImageFormat.Jpeg: return "JPG";
                case SKEncodedImageFormat.Png: return "PNG";
                default: return "JPG";
            }
        }

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

        public static byte[] GetResizedImage(SKBitmap image, int width, int height)
        {
            if (image == null)
            {
                return null;
            }

            int targetWidth = width;
            int targetHeight = height;

            if (targetWidth.Equals(0))
            {
                targetWidth = image.Width;
            }

            if (targetHeight.Equals(0))
            {
                targetHeight = image.Height;
            }

            float scalingRatio = CalculateScalingRatio(image.Width, image.Height, targetWidth, targetHeight);

            int scaledWidth = (int)Math.Round(image.Width * scalingRatio);
            int scaledHeight = (int)Math.Round(image.Height * scalingRatio);
            int scaledLeft = (targetWidth - scaledWidth) / 2;
            int scaledTop = (targetHeight - scaledHeight) / 2;

            // For portrait mode, adjust the vertical top of the crop area so that we get more of
            // the top area
            if (scaledWidth < scaledHeight && scaledHeight > targetHeight)
            {
                scaledTop = (targetHeight - scaledHeight) / 4;
            }

            using (SKBitmap thumbnail = new SKBitmap(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul))
            {
                using (SKCanvas canvas = new SKCanvas(thumbnail))
                {
                    canvas.Clear(SKColors.Transparent);

                    using (var resized = image.Resize(new SKImageInfo(scaledWidth, scaledHeight), new SKSamplingOptions(SKCubicResampler.Mitchell)))
                    {
                        if (resized != null)
                        {
                            canvas.DrawBitmap(resized, scaledLeft, scaledTop);
                        }
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