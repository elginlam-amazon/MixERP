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

using MixERP.Net.Common.Helpers;
using MixERP.Net.i18n;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MixERP.Net.Common.jQueryHelper
{
    public static class jQueryUI
    {
        public static void AddjQueryUIDatePicker(dynamic page, string id, string catalog, string resourceClassName) { }
        public static string GetDatePickerFormat()
        {
            string datePattern = CurrentCulture.GetCurrentUICulture().DateTimeFormat.ShortDatePattern;
            return ConvertDateFormat(datePattern);
        }

        public static string GetDatePickerLocale()
        {
            return CurrentCulture.GetCurrentUICulture().TwoLetterISOLanguageName;
        }

        public static string GetNumberOfMonths()
        {
            return ConfigurationHelper.GetParameter("DatePickerNumberOfMonths");
        }

        public static int GetWeekStartDay()
        {
            return (int)CurrentCulture.GetCurrentUICulture().DateTimeFormat.FirstDayOfWeek;
        }

        public static bool ShowWeekNumber()
        {
            return ConfigurationHelper.GetParameter("DatePickerShowWeekNumber").Equals("true");
        }

        private static string ConvertDateFormat(string format)
        {
            string currentFormat = format;

            currentFormat = currentFormat.Replace("dddd", "DD");
            currentFormat = currentFormat.Replace("ddd", "D");

            if (currentFormat.Contains("MMMM"))
            {
                currentFormat = currentFormat.Replace("MMMM", "MM");
            }
            else if (currentFormat.Contains("MMM"))
            {
                currentFormat = currentFormat.Replace("MMM", "M");
            }
            else if (currentFormat.Contains("MM"))
            {
                currentFormat = currentFormat.Replace("MM", "mm");
            }
            else
            {
                currentFormat = currentFormat.Replace("M", "m");
            }

            currentFormat = currentFormat.Contains("yyyy") ? currentFormat.Replace("yyyy", "yy") : currentFormat.Replace("yy", "y");

            return currentFormat;
        }
    }
}
