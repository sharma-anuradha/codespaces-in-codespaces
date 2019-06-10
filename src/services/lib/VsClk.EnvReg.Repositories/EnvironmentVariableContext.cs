using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VsClk.EnvReg.Models.DataStore.Compute;

namespace VsClk.EnvReg.Repositories
{
    public static class EnvironmentVariableConstants
    {
        public const string GitRepoUrl = "GIT_REPO_URL";
        public const string GitPRNumber = "GIT_PR_NUM";
        public const string GitConfigUsername = "GIT_CONFIG_USER_NAME";
        public const string GitConfigUserEmail = "GIT_CONFIG_USER_EMAIL";
        public const string SessionCallback = "SESSION_CALLBACK";
        public const string SessionToken = "SESSION_TOKEN";
    }

    public class EnvironmentVariableGenerator
    {
        public static List<EnvironmentVariable> Generate(EnvironmentRegistration environmentRegistration, AppSettings appSettings, string accessToken)
        {
            List<EnvironmentVariable> result = new List<EnvironmentVariable>();

            var list = new EnvironmentVariableStrategy[]
            {
                new EnvVarGitRepoUrl(environmentRegistration),
                new EnvVarGitPullPRNumber(environmentRegistration),
                new EnvVarGitConfigUserName(environmentRegistration),
                new EnvVarGitConfigUserEmail(environmentRegistration),
                new EnvVarSessionCallback(environmentRegistration, appSettings),
                new EnvVarSessionToken(accessToken)
            };

            foreach (var envStrategy in list)
            {
                EnvironmentVariableContext context = new EnvironmentVariableContext(envStrategy);
                var envVar = context.ExecuteStrategy();
                if (envVar != null)
                {
                    result.Add(envVar);
                }
            }

            return result;
        }
    }

    public class EnvironmentVariableContext
    {
        private readonly EnvironmentVariableStrategy strategy;

        public EnvironmentVariableContext(EnvironmentVariableStrategy strategy)
        {
            this.strategy = strategy;
        }

        public EnvironmentVariable ExecuteStrategy()
        {
            return this.strategy.GetEnvironmentVariable();
        }
    }

    public abstract class EnvironmentVariableStrategy
    {
        public EnvironmentRegistration EnvironmentRegistration { get; }

        public EnvironmentVariableStrategy(EnvironmentRegistration environmentRegistration)
        {
            this.EnvironmentRegistration = environmentRegistration;
        }

        public abstract EnvironmentVariable GetEnvironmentVariable();

        protected static bool IsValidGitUrl(string url)
        {
            string regex = "(?:https?):\\/\\/(github\\.com)\\/(\\w+(-\\w+)*)\\/(\\w+(-\\w+)*)((\\/$|\\/(tree|pull|commit|releases\\/tag)\\/\\d+($|\\/))|$)";
            return Regex.IsMatch(url, regex);
        }
    }

    public class EnvVarGitRepoUrl : EnvironmentVariableStrategy
    {
        public EnvVarGitRepoUrl(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Seed != null
                && EnvironmentRegistration.Seed.SeedType == "git"
                && IsValidGitUrl(EnvironmentRegistration.Seed.SeedMoniker))
            {
                var moniker = EnvironmentRegistration.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    return new EnvironmentVariable(EnvironmentVariableConstants.GitRepoUrl, repoUrl[0]);
                }
                else
                {
                    return new EnvironmentVariable(EnvironmentVariableConstants.GitRepoUrl, moniker);
                }
            }

            return null;
        }
    }

    public class EnvVarGitPullPRNumber : EnvironmentVariableStrategy
    {
        public EnvVarGitPullPRNumber(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Seed != null
                && EnvironmentRegistration.Seed.SeedType == "git"
                && IsValidGitUrl(EnvironmentRegistration.Seed.SeedMoniker))
            {
                var moniker = EnvironmentRegistration.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    return new EnvironmentVariable(EnvironmentVariableConstants.GitPRNumber, Regex.Match(repoUrl[1], "(\\d+)").ToString());
                }
            }

            return null;
        }
    }

    public class EnvVarGitConfigUserName : EnvironmentVariableStrategy
    {
        public EnvVarGitConfigUserName(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Seed != null && EnvironmentRegistration.Seed.GitConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(EnvironmentRegistration.Seed.GitConfig.UserName))
                {
                    return new EnvironmentVariable(EnvironmentVariableConstants.GitConfigUsername, EnvironmentRegistration.Seed.GitConfig.UserName);
                }
            }

            return null;
        }
    }

    public class EnvVarGitConfigUserEmail : EnvironmentVariableStrategy
    {
        public EnvVarGitConfigUserEmail(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Seed != null && EnvironmentRegistration.Seed.GitConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(EnvironmentRegistration.Seed.GitConfig.UserEmail))
                {
                    return new EnvironmentVariable(EnvironmentVariableConstants.GitConfigUserEmail, EnvironmentRegistration.Seed.GitConfig.UserEmail);
                }
            }

            return null;
        }
    }

    public class EnvVarSessionCallback : EnvironmentVariableStrategy
    {
        public AppSettings AppSettings { get; }

        public EnvVarSessionCallback(EnvironmentRegistration environmentRegistration, AppSettings appSettings) : base(environmentRegistration)
        {
            AppSettings = appSettings;
        }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            string apiUrl = AppSettings.PreferredSchema + "://" + AppSettings.DefaultHost + AppSettings.DefaultPath + "/registration/";
            return new EnvironmentVariable(EnvironmentVariableConstants.SessionCallback, apiUrl + EnvironmentRegistration.Id + "/_callback");
        }
    }

    public class EnvVarSessionToken : EnvironmentVariableStrategy
    {
        private readonly string accessToken;

        public EnvVarSessionToken(string accessToken) : base(null)
        {
            this.accessToken = accessToken;
        }
        
        public override EnvironmentVariable GetEnvironmentVariable()
        {
            return new EnvironmentVariable(EnvironmentVariableConstants.SessionToken, this.accessToken);
        }
    }
}
