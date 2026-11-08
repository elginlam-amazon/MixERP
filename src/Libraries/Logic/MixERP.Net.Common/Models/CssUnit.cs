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
using System.Globalization;

namespace MixERP.Net.Common
{
    /// <summary>
    ///     Framework-neutral replacement for the removed
    ///     <c>System.Web.UI.WebControls.Unit</c> value type. It is a CSS length: a numeric
    ///     value plus a unit type, with an "empty" state and a <see cref="ToString()" />
    ///     that renders exactly what the old <c>Unit.ToString()</c> rendered
    ///     (for example <c>10px</c>, <c>50%</c>, <c>1.5em</c>, or an empty string when empty).
    /// </summary>
    public struct CssUnit : IEquatable<CssUnit>
    {
        private readonly bool hasValue;
        private readonly double value;
        private readonly CssUnitType type;

        private CssUnit(double value, CssUnitType type)
        {
            this.hasValue = true;
            this.value = value;
            this.type = type;
        }

        /// <summary>
        ///     An empty CSS length, equivalent to <c>Unit.Empty</c>. Renders as an empty string.
        /// </summary>
        public static CssUnit Empty
        {
            get { return new CssUnit(); }
        }

        /// <summary>
        ///     True when this instance carries no value (equivalent to <c>Unit.IsEmpty</c>).
        /// </summary>
        public bool IsEmpty
        {
            get { return !this.hasValue; }
        }

        /// <summary>
        ///     The numeric part of the length. Zero when empty.
        /// </summary>
        public double Value
        {
            get { return this.value; }
        }

        /// <summary>
        ///     The unit part of the length.
        /// </summary>
        public CssUnitType Type
        {
            get { return this.hasValue ? this.type : CssUnitType.Pixel; }
        }

        public static CssUnit Pixel(int n)
        {
            return new CssUnit(n, CssUnitType.Pixel);
        }

        public static CssUnit Percentage(double n)
        {
            return new CssUnit(n, CssUnitType.Percentage);
        }

        public static CssUnit Point(int n)
        {
            return new CssUnit(n, CssUnitType.Point);
        }

        /// <summary>
        ///     Parses a CSS length, mirroring the behaviour of the removed <c>Unit.Parse</c>:
        ///     a null/whitespace input yields <see cref="Empty" />, a bare number is interpreted
        ///     as pixels, a recognized suffix selects the unit type, and an unparseable input
        ///     throws <see cref="FormatException" /> (exactly as <c>Unit.Parse</c> did).
        /// </summary>
        public static CssUnit Parse(string text, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Empty;
            }

            CultureInfo effective = culture ?? CultureInfo.InvariantCulture;
            string trimmed = text.Trim();

            CssUnitType unitType = CssUnitType.Pixel;
            string numericPart = trimmed;

            if (trimmed.EndsWith("%", StringComparison.Ordinal))
            {
                unitType = CssUnitType.Percentage;
                numericPart = trimmed.Substring(0, trimmed.Length - 1);
            }
            else
            {
                int suffixStart = trimmed.Length;

                while (suffixStart > 0 && char.IsLetter(trimmed[suffixStart - 1]))
                {
                    suffixStart--;
                }

                if (suffixStart < trimmed.Length)
                {
                    string suffix = trimmed.Substring(suffixStart);
                    numericPart = trimmed.Substring(0, suffixStart);
                    unitType = ParseUnitType(suffix);
                }
            }

            double parsed;

            if (!double.TryParse(numericPart.Trim(), NumberStyles.Float, effective, out parsed))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid CSS length.", text));
            }

            //Pixel, point, pica, mm, cm and in lengths are integral, exactly as the old Unit type was.
            if (unitType != CssUnitType.Percentage && unitType != CssUnitType.Em && unitType != CssUnitType.Ex)
            {
                parsed = Math.Truncate(parsed);
            }

            return new CssUnit(parsed, unitType);
        }

        public static CssUnit Parse(string text)
        {
            return Parse(text, CultureInfo.InvariantCulture);
        }

        private static CssUnitType ParseUnitType(string suffix)
        {
            switch (suffix.ToLowerInvariant())
            {
                case "px":
                    return CssUnitType.Pixel;
                case "pt":
                    return CssUnitType.Point;
                case "pc":
                    return CssUnitType.Pica;
                case "em":
                    return CssUnitType.Em;
                case "ex":
                    return CssUnitType.Ex;
                case "mm":
                    return CssUnitType.Mm;
                case "cm":
                    return CssUnitType.Cm;
                case "in":
                    return CssUnitType.Inch;
                default:
                    throw new FormatException(string.Format(CultureInfo.InvariantCulture, "'{0}' is not a recognized CSS unit.", suffix));
            }
        }

        private string GetSuffix()
        {
            switch (this.type)
            {
                case CssUnitType.Percentage:
                    return "%";
                case CssUnitType.Point:
                    return "pt";
                case CssUnitType.Pica:
                    return "pc";
                case CssUnitType.Em:
                    return "em";
                case CssUnitType.Ex:
                    return "ex";
                case CssUnitType.Mm:
                    return "mm";
                case CssUnitType.Cm:
                    return "cm";
                case CssUnitType.Inch:
                    return "in";
                default:
                    return "px";
            }
        }

        public override string ToString()
        {
            return this.ToString(CultureInfo.InvariantCulture);
        }

        public string ToString(CultureInfo culture)
        {
            if (!this.hasValue)
            {
                return string.Empty;
            }

            CultureInfo effective = culture ?? CultureInfo.InvariantCulture;
            return this.value.ToString("0.######", effective) + this.GetSuffix();
        }

        public bool Equals(CssUnit other)
        {
            if (!this.hasValue || !other.hasValue)
            {
                return this.hasValue == other.hasValue;
            }

            return this.value.Equals(other.value) && this.type == other.type;
        }

        public override bool Equals(object obj)
        {
            return obj is CssUnit && this.Equals((CssUnit)obj);
        }

        public override int GetHashCode()
        {
            return this.hasValue ? this.value.GetHashCode() ^ (int)this.type : 0;
        }

        public static bool operator ==(CssUnit left, CssUnit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CssUnit left, CssUnit right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    ///     The CSS unit types the old <c>System.Web.UI.WebControls.UnitType</c> enumeration carried.
    /// </summary>
    public enum CssUnitType
    {
        Pixel = 1,
        Point = 2,
        Pica = 3,
        Inch = 4,
        Mm = 5,
        Cm = 6,
        Percentage = 7,
        Em = 8,
        Ex = 9
    }
}
