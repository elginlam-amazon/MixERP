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

using MixERP.Net.i18n;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MixERP.Net.Common.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace MixERP.Net.Common
{
    public static class Conversion
    {
        [System.CLSCompliant(false)]
        public static byte[] ConvertImageToByteArray(Image imageToConvert, IImageFormat formatOfImage)
        {
            if (imageToConvert == null)
            {
                return null;
            }

            IImageEncoder encoder = ImageHelper.GetEncoder(formatOfImage) ??
                                    ImageHelper.GetEncoder(ImageHelper.GetSourceFormat(imageToConvert));

            using (MemoryStream ms = new MemoryStream())
            {
                imageToConvert.Save(ms, encoder);
                return ms.ToArray();
            }
        }

        public static DateTime GetLocalDateTime(string timeZone, DateTime utc)
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        }

        public static string GetLocalDateTimeString(string timeZone, DateTime utc)
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime time = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            return String.Format(CurrentCulture.GetCurrentUICulture(), "{0} {1} {2}", time.ToLongDateString(), time.ToLongTimeString(), zone.DisplayName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public static string HashSha512Hex(string password, string salt)
        {
            if (password == null)
            {
                return null;
            }

            if (salt == null)
            {
                return null;
            }

            using (SHA512 provider = SHA512.Create())
            {
                return BitConverter.ToString(provider.ComputeHash(Encoding.UTF8.GetBytes(password + salt))).Replace("-", "").ToLower(CultureInfo.InvariantCulture);
            }
        }

        public static bool IsEmptyDate(DateTime date)
        {
            if (date.Equals(DateTime.MinValue))
            {
                return true;
            }

            return false;
        }

        public static bool IsNumeric(string value)
        {
            double number;
            return double.TryParse(value, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out number);
        }

        public static string MapPathReverse(string fullServerPath)
        {
            if (string.IsNullOrWhiteSpace(fullServerPath))
            {
                return null;
            }
            string physicalApplicationPath = MixERPConfiguration.ContentRootPath;

            if (string.IsNullOrWhiteSpace(physicalApplicationPath))
            {
                return null;
            }

            if (!physicalApplicationPath.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                physicalApplicationPath += Path.DirectorySeparatorChar;
            }

            return @"~/" + fullServerPath.Replace(physicalApplicationPath, String.Empty).Replace(@"\", "/");
        }

        public static string ResolveUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            return PageUtility.ResolveUrl(url);
        }

        public static bool TryCastBoolean(object value)
        {
            bool retVal = false;

            if (value != null)
            {
                if (value is string)
                {
                    if (value.ToString().ToUpperInvariant().Equals("YES"))
                    {
                        return true;
                    }

                    if (value.ToString().ToUpperInvariant().Equals("TRUE"))
                    {
                        return true;
                    }
                }

                if (bool.TryParse(value.ToString(), out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        [System.CLSCompliant(false)]
        public static byte[] TryCastByteArray(Image image)
        {
            if (image == null)
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                //Preserve the source format instead of forcing JPEG, so PNG transparency
                //and GIF animation survive the round trip.
                image.Save(ms, ImageHelper.GetEncoder(ImageHelper.GetSourceFormat(image)));
                return ms.ToArray();
            }
        }

        public static DateTime TryCastDate(object value)
        {
            try
            {
                if (value == DBNull.Value)
                {
                    return DateTime.MinValue;
                }

                if (value is DateTime)
                {
                    return (DateTime) value;
                }

                //Npgsql 6+ maps PostgreSQL "date" to DateOnly and "time" to TimeOnly.
                //Neither is IConvertible, so Convert.ToDateTime would throw and collapse
                //the value to DateTime.MinValue. Map them explicitly to preserve the
                //DateTime semantics callers had under Npgsql 2.x.
                if (value is DateOnly dateOnly)
                {
                    return dateOnly.ToDateTime(TimeOnly.MinValue);
                }

                if (value is TimeOnly timeOnly)
                {
                    return DateTime.MinValue.Add(timeOnly.ToTimeSpan());
                }

                return Convert.ToDateTime(value, CultureInfo.DefaultThreadCurrentCulture);
            }
            catch (FormatException)
            {
                Log.Debug("Invalid date format: {Value}.", value);
            }
            catch (InvalidCastException)
            {
                Log.Debug("Could not cast {Value} to date.", value);
            }

            return DateTime.MinValue;
        }

        public static decimal TryCastDecimal(object value)
        {
            decimal retVal = 0;

            if (value != null)
            {
                if (value is decimal)
                {
                    return (decimal)value;
                }

                string numberToParse = value.ToString();


                if (decimal.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static decimal TryCastDecimal(object value, CultureInfo culture)
        {
            decimal retVal = 0;

            if (value != null)
            {
                if (value is decimal)
                {
                    return (decimal)value;
                }

                string numberToParse = value.ToString();


                if (decimal.TryParse(numberToParse, NumberStyles.Any, culture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static double TryCastDouble(object value)
        {
            double retVal = 0;

            if (value != null)
            {
                if (value is double)
                {
                    return (double) value;
                }

                string numberToParse = value.ToString();

                if (double.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static int TryCastInteger(object value)
        {
            int retVal = 0;

            if (value != null)
            {
                if (value is bool)
                {
                    if (Convert.ToBoolean(value, CultureInfo.InvariantCulture))
                    {
                        return 1;
                    }
                }

                if (value is int)
                {
                    return (int) value;
                }


                string numberToParse = value.ToString();

                if (int.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static long TryCastLong(object value)
        {
            long retVal = 0;

            if (value != null)
            {
                if (value is long)
                {
                    return (long) value;
                }

                string numberToParse = value.ToString();

                if (long.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static short TryCastShort(object value)
        {
            short retVal = 0;

            if (value != null)
            {
                if (value is short)
                {
                    return (short) value;
                }

                string numberToParse = value.ToString();

                if (short.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        public static float TryCastSingle(object value)
        {
            float retVal = 0;

            if (value != null)
            {
                if (value is float)
                {
                    return (float) value;
                }

                string numberToParse = value.ToString();

                if (float.TryParse(numberToParse, NumberStyles.Any, CultureInfo.DefaultThreadCurrentCulture, out retVal))
                {
                    return retVal;
                }
            }

            return retVal;
        }

        //    return number;
        //}
        public static string TryCastString(object value)
        {
            if (value != null)
            {
                if (value is bool)
                {
                    if (Convert.ToBoolean(value, CultureInfo.InvariantCulture))
                    {
                        return "true";
                    }

                    return "false";
                }

                if (value == DBNull.Value)
                {
                    return string.Empty;
                }

                string retVal = value.ToString();
                return retVal;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Casts a value to a CSS length.
        ///     <para>
        ///         SIGNATURE CHANGE: the return type was <c>System.Web.UI.WebControls.Unit</c>,
        ///         which does not exist on modern .NET; it is now the framework-neutral
        ///         <see cref="CssUnit" /> struct, which keeps the value-type semantics and the
        ///         <c>ToString()</c> output ("10px", "50%", …) consumers rely on. The parse
        ///         behaviour is unchanged, including throwing on unparseable input.
        ///     </para>
        /// </summary>
        public static CssUnit TryCastUnit(object value)
        {
            if (value == null)
            {
                return new CssUnit();
            }

            if (value is CssUnit)
            {
                return (CssUnit) value;
            }


            return CssUnit.Parse(value.ToString(), CultureInfo.DefaultThreadCurrentCulture);
        }

        public static DateTime? TryCastNullableDate(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                return null;
            }

            return Convert.ToDateTime(value, CultureInfo.DefaultThreadCurrentCulture);
        }
    }
}