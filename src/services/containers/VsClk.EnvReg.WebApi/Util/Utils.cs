using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using System.Text.RegularExpressions;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Util
{
    public class Utils
    {
        public static bool IsCreateInputValid(EnvironmentRegistrationInput input)
        {
            return !(string.IsNullOrEmpty(input.FriendlyName) || string.IsNullOrEmpty(input.Type));
        }
        public static bool IsValidGitUrl(string url)
        {
            string regex = "(?:https?):\\/\\/(github\\.com)\\/(\\w+(-\\w+)*)\\/(\\w+(-\\w+)*)((\\/$|\\/(tree|pull|commit|releases\\/tag)\\/\\d+($|\\/))|$)";
            return Regex.IsMatch(url, regex);
        }
    }
}
