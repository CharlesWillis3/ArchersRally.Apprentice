// <copyright file="StringArrayTypeConverter.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.Utilities
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Converts an array of strings to a semi-colon delimited string for serialization, and back again.
    /// </summary>
    public class StringArrayTypeConverter : TypeConverter
    {
        private const string Delimiter = ";";

        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string[]) || base.CanConvertTo(context, destinationType);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string v = value as string;

            return v == null ? base.ConvertFrom(context, culture, value) : v.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var v = value as string[];
            if (destinationType != typeof(string) || v == null)
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }

            return string.Join(Delimiter, v);
        }
    }
}
