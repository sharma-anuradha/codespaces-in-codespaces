namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class UndefineToken : Token
    {
        public const string Token = "#undef";

        public string Undefine
        {
            get;
            set;
        }

        public static UndefineToken Extract(string line)
        {
            line = line.TrimStart();
            line = line.Remove(0, Token.Length);
            line = line.Remove(0, 2); // Remove bracket and quotes. (" 
            var endingQuoteAt = line.IndexOf('"'); // Find ending quote.
            line = line.Remove(endingQuoteAt);

            return new UndefineToken()
            {
                Undefine = line,
            };
        }
    }
}
