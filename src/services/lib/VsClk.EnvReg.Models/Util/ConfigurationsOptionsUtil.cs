using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.Util
{
    public static class ConfigurationsOptionsUtil
    {
        public static IOptionsSnapshot<TOptions> PromoteToOptionSnapshot<TOptions>(this IOptions<TOptions> option)
            where TOptions : class, new()
        {
            return new DirectOptionsSnapshot<TOptions>(option.Value);
        }

        private class DirectOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
        {
            public DirectOptionsSnapshot(TOptions options)
            {
                Options = options;
            }
            private TOptions Options { get; }

            public TOptions Value => Options;
            
            public TOptions Get(string name)
            {
                return Options;
            }
        }
    }
}
