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
using System.Drawing.Imaging;
using System.IO;

namespace MixERP.Net.Common.Helpers
{
    public static class ImageHelper
    {
        public static string GetContentType(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".bmp": return "Image/bmp";
                case ".gif": return "Image/gif";
                case ".jpg": return "Image/jpeg";
                case ".jpeg": return "Image/jpeg";
                case ".png": return "Image/png";
                default: return "text/plain";
            }
        }

        public static byte[] GetResizedImage(string path, int width, int height)
        {
            if (string.IsNullOrEmpty(path)) return null;
            using var original = SKBitmap.Decode(path);
            return ResizeCore(original, width, height);
        }

        public static byte[] GetResizedImage(byte[] data, int width, int height)
        {
            if (data == null || data.Length == 0) return null;
            using var original = SKBitmap.Decode(data);
            return ResizeCore(original, width, height);
        }

        private static byte[] ResizeCore(SKBitmap original, int width, int height)
        {
            if (original == null) return null;

            int targetW = width == 0 ? original.Width : width;
            int targetH = height == 0 ? original.Height : height;

            float ratio = CalculateScalingRatio(original.Width, original.Height, targetW, targetH);
            int scaledW = (int)Math.Round(original.Width * ratio);
            int scaledH = (int)Math.Round(original.Height * ratio);
            int left = (targetW - scaledW) / 2;
            int top = (targetH - scaledH) / 2;

            if (scaledW < scaledH && scaledH > targetH)
                top = (targetH - scaledH) / 4;

            using var thumbnail = new SKBitmap(targetW, targetH);
            using var canvas = new SKCanvas(thumbnail);
            canvas.Clear(SKColors.Transparent);
            var dest = new SKRect(left, top, left + scaledW, top + scaledH);
            canvas.DrawBitmap(original, dest);

            using var image = SKImage.FromBitmap(thumbnail);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

#pragma warning disable CA1416
        public static ImageFormat GetImageFormat(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".bmp": return ImageFormat.Bmp;
                case ".gif": return ImageFormat.Gif;
                case ".jpg": return ImageFormat.Jpeg;
                case ".png": return ImageFormat.Png;
                default: return ImageFormat.Jpeg;
            }
        }
#pragma warning restore CA1416

        private static float CalculateScalingRatio(int origW, int origH, int targetW, int targetH)
        {
            float origAR = (float)origW / origH;
            float targetAR = (float)targetW / targetH;
            return targetAR >= origAR ? (float)targetW / origW : (float)targetH / origH;
        }
    }
}
