namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class IncludeFileToken : Token
    {
        public const string IncludeToken = "#include";

        public string File
        {
            get;
            set;
        }

        public static IncludeFileToken Extract(string line)
        {
            line = line.TrimStart();
            line = line.Remove(0, IncludeToken.Length);
            line = line.Remove(0, 2); // Remove bracket and quotes. (" 
            var endingQuoteAt = line.IndexOf('"'); // Find ending quote.
            line = line.Remove(endingQuoteAt);

            return new IncludeFileToken()
            {
                File = line,
            };
        }
    }
}
