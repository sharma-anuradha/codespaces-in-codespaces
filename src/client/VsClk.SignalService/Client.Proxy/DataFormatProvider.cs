// <copyright file="DataFormatProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Implements IDataFormatProvider from an existing IFormatProvider.
    /// </summary>
    internal class DataFormatProvider : IDataFormatProvider
    {
        private readonly IFormatProvider formatProvider;

        private DataFormatProvider(IFormatProvider formatProvider)
        {
            this.formatProvider = Requires.NotNull(formatProvider, nameof(formatProvider));
        }

        public static IDataFormatProvider Create(IFormatProvider formatProvider)
        {
            if (formatProvider is ICustomFormatter customFormatter)
            {
                return new CustomFormatter(customFormatter, formatProvider);
            }

            return new DataFormatProvider(formatProvider);
        }

        /// <inheritdoc/>
        public object GetFormat(Type formatType)
        {
            return this.formatProvider.GetFormat(formatType);
        }

        /// <inheritdoc/>
        public string GetPropertyFormat(string propertyName)
        {
            return FormatHelpers.GetPropertyFormat(propertyName);
        }

        private class CustomFormatter : DataFormatProvider, ICustomFormatter
        {
            private readonly ICustomFormatter customFormatter;

            internal CustomFormatter(ICustomFormatter customFormatter, IFormatProvider formatProvider)
                : base(formatProvider)
            {
                this.customFormatter = customFormatter;
            }

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return this.customFormatter.Format(format, arg, formatProvider);
            }
        }
    }
}
