namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Hub helpers
    /// </summary>
    public static class HubHelpers
    {
        public static string ToCamelCase(this string name)
        {
            Requires.NotNull(name, nameof(name));

            return name.Length == 0 ? string.Empty :
                name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }
    }
}
