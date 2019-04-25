using System.Text.RegularExpressions;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationInput
    {
        public string Type { get; set; }

        public string FriendlyName { get; set; }

        public SeedInfo Seed { get; set; }

        public string ContainerImage { get; set; }

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