﻿using Microsoft.VsCloudKernel.Services.EnvReg.Models;
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
        public const string SessionId = "SESSION_ID";
        public const string DotfilesRepository = "DOTFILES_REPOSITORY";
        public const string DotfilesTargetPath = "DOTFILES_REPOSITORY_TARGET";
        public const string DotfilesInstallCommand = "DOTFILES_INSTALL_COMMAND";
        public const string KitchenSinkDefaultShell = "SHELL";
        public const string CustomImageDefaultShell = "CLOUDENV_SHELL";
    }

    public class EnvironmentVariableGenerator
    {
        public static List<EnvironmentVariable> Generate(EnvironmentRegistration environmentRegistration, AppSettings appSettings, string accessToken, string sessionId)
        {
            List<EnvironmentVariable> result = new List<EnvironmentVariable>();

            var list = new EnvironmentVariableStrategy[]
            {
                new EnvVarGitRepoUrl(environmentRegistration),
                new EnvVarGitPullPRNumber(environmentRegistration),
                new EnvVarGitConfigUserName(environmentRegistration),
                new EnvVarGitConfigUserEmail(environmentRegistration),
                new EnvVarSessionCallback(environmentRegistration, appSettings),
                new EnvVarSessionToken(accessToken),
                new EnvVarSessionId(sessionId),
                new EnvDotfilesRepoUrl(environmentRegistration),
                new EnvDotfilesTargetPath(environmentRegistration),
                new EnvDotfilesInstallCommand(environmentRegistration),
                new EnvDefaultShell(environmentRegistration)
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
            string regex = @"((^https:\/\/github.com\/([a-zA-Z0-9-_~.\/])+)((.git)*|((tree|pull|commit|releases\/tag)([a-zA-Z0-9-_~.]+)+)))$";
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

    public class EnvVarSessionId : EnvironmentVariableStrategy
    {
        private readonly string sessionId;

        public EnvVarSessionId(string sessionId) : base(null)
        {
            this.sessionId = sessionId;
        }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            return new EnvironmentVariable(EnvironmentVariableConstants.SessionId, this.sessionId);
        }
    }

    public class EnvDotfilesRepoUrl : EnvironmentVariableStrategy
    {
        public EnvDotfilesRepoUrl(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Personalization != null
                && !string.IsNullOrWhiteSpace(EnvironmentRegistration.Personalization.DotfilesRepository)
                && IsValidGitUrl(EnvironmentRegistration.Personalization.DotfilesRepository))
            {
                return new EnvironmentVariable(
                    EnvironmentVariableConstants.DotfilesRepository,
                    EnvironmentRegistration.Personalization.DotfilesRepository);
            }

            return null;
        }
    }

    public class EnvDotfilesTargetPath : EnvironmentVariableStrategy
    {
        public EnvDotfilesTargetPath(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Personalization != null
                && !string.IsNullOrWhiteSpace(EnvironmentRegistration.Personalization.DotfilesTargetPath))
            {
                return new EnvironmentVariable(
                    EnvironmentVariableConstants.DotfilesTargetPath,
                    EnvironmentRegistration.Personalization.DotfilesTargetPath);
            }

            return null;
        }
    }

    public class EnvDotfilesInstallCommand : EnvironmentVariableStrategy
    {
        public EnvDotfilesInstallCommand(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.Personalization != null
                && !string.IsNullOrWhiteSpace(EnvironmentRegistration.Personalization.DotfilesInstallCommand))
            {
                return new EnvironmentVariable(
                    EnvironmentVariableConstants.DotfilesInstallCommand,
                    EnvironmentRegistration.Personalization.DotfilesInstallCommand);
            }

            return null;
        }
    }

    public class EnvDefaultShell : EnvironmentVariableStrategy
    {
        private static readonly Dictionary<string, string> AvailableShells = new Dictionary<string, string>()
        {
            { "bash", "/bin/bash" },
            { "fish", "/usr/bin/fish" },
            { "zsh", "/usr/bin/zsh" }
        };

        public EnvDefaultShell(EnvironmentRegistration environmentRegistration) : base(environmentRegistration)
        { }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            if (EnvironmentRegistration.ContainerImage == "kitchensink")
            {
                // If it's the kitchen sink image, we already know the location of shell.
                // Use that to directly set the default shell on linux container when the container is being created.
                var supportedShell = GetSupportedShell(true);
                return !string.IsNullOrWhiteSpace(supportedShell) ?
                   new EnvironmentVariable(EnvironmentVariableConstants.KitchenSinkDefaultShell, supportedShell) :
                   null;
            }
            else
            {
                // If it's a custom container image, we need to locate the shell path on the container after it has been created.
                // Send the list of preferred shells to the container, and let the bootstrap cli set the correct default shell.
                var supportedShells = GetSupportedShell(false);
                return !string.IsNullOrWhiteSpace(supportedShells) ?
                    new EnvironmentVariable(EnvironmentVariableConstants.CustomImageDefaultShell, supportedShells) :
                    null;
            }
        }

        private string GetSupportedShell(bool isKitchenSink)
        {
            // The value for this is programatically filled so doesn't need normalization.
            if (EnvironmentRegistration.Personalization.PreferredShells != null)
            {
                if (isKitchenSink)
                {
                    foreach (var shell in EnvironmentRegistration.Personalization.PreferredShells)
                    {
                        if (shell != null && AvailableShells.TryGetValue(shell, out var kitchenSinkShell))
                        {
                            return kitchenSinkShell;
                        }
                    }
                }
                else
                {
                    var shells = string.Empty;
                    foreach (var shell in EnvironmentRegistration.Personalization.PreferredShells)
                    {
                        if (!string.IsNullOrWhiteSpace(shell))
                        {
                            shells += shell + ',';
                        }
                    }

                    // Remove the last ','
                    return shells.Length == 0 ? shells : shells.Substring(0, shells.Length - 1);
                }
            }

            return null;
        }
    }
}
