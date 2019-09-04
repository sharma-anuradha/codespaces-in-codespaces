using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    internal class HubFormatProvider : IHubFormatProvider
    {
        private readonly IFormatProvider formatProvider;

        private HubFormatProvider(IFormatProvider formatProvider)
        {
            this.formatProvider = Requires.NotNull(formatProvider, nameof(formatProvider));
        }

        public static IHubFormatProvider Create(IFormatProvider formatProvider)
        {
            if (formatProvider is ICustomFormatter customFormatter)
            {
                return new CustomFormatter(customFormatter, formatProvider);
            }

            return new HubFormatProvider(formatProvider);
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

        private class CustomFormatter : HubFormatProvider, ICustomFormatter
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
