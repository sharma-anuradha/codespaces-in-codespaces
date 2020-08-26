namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class DefineToken : Token
    {
        public const string Token = "#define";

        public string Define
        {
            get;
            set;
        }

        public static DefineToken Extract(string line)
        {
            line = line.TrimStart();
            line = line.Remove(0, Token.Length);
            line = line.Remove(0, 2); // Remove bracket and quotes. (" 
            var endingQuoteAt = line.IndexOf('"'); // Find ending quote.
            line = line.Remove(endingQuoteAt);

            return new DefineToken()
            {
                Define = line,
            };
        }
    }
}
