﻿// <copyright file="EnvironmentVariableGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories
{
    /// <summary>
    /// The environment variable generator.
    /// </summary>
    public class EnvironmentVariableGenerator
    {
        /// <summary>
        /// Generates a dictionary of environment variables for the given cloud environment.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="sessionSettings">The session settings.</param>
        /// <param name="accessToken">The user access token.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>A dictionary of environment variables.</returns>
        public static Dictionary<string, string> Generate(CloudEnvironment cloudEnvironment, SessionSettings sessionSettings, string accessToken, string sessionId)
        {
            var result = new Dictionary<string, string>();

            var list = new EnvironmentVariableStrategy[]
            {
                new EnvVarGitRepoUrl(cloudEnvironment),
                new EnvVarGitPullPRNumber(cloudEnvironment),
                new EnvVarGitConfigUserName(cloudEnvironment),
                new EnvVarGitConfigUserEmail(cloudEnvironment),
                new EnvVarSessionCallback(cloudEnvironment, sessionSettings),
                new EnvVarSessionToken(accessToken),
                new EnvVarSessionId(sessionId),
                new EnvDotfilesRepoUrl(cloudEnvironment),
                new EnvDotfilesTargetPath(cloudEnvironment),
                new EnvDotfilesInstallCommand(cloudEnvironment),
                new EnvDefaultShell(cloudEnvironment),
            };

            foreach (var envStrategy in list)
            {
                var envVar = new EnvironmentVariableContext(envStrategy).ExecuteStrategy();
                if (envVar != null)
                {
                    result.Add(envVar.Item1, envVar.Item2);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Environment variable generation context.
    /// </summary>
    public class EnvironmentVariableContext
    {
        private readonly EnvironmentVariableStrategy strategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableContext"/> class.
        /// </summary>
        /// <param name="strategy">The environment variable strategy to invoke.</param>
        public EnvironmentVariableContext(EnvironmentVariableStrategy strategy)
        {
            this.strategy = strategy;
        }

        /// <summary>
        /// Invoke the strategy.
        /// </summary>
        /// <returns>An environment variable tuple.</returns>
        public Tuple<string, string> ExecuteStrategy()
        {
            return strategy.GetEnvironmentVariable();
        }
    }

    /// <summary>
    /// Base environment variable strategy.
    /// </summary>
    public abstract class EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableStrategy"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvironmentVariableStrategy(CloudEnvironment cloudEnvironment)
        {
            CloudEnvironment = cloudEnvironment;
        }

        /// <summary>
        /// Gets the cloud environment instance.
        /// </summary>
        public CloudEnvironment CloudEnvironment { get; }

        /// <summary>
        /// Get the environment variable for this strategy.
        /// </summary>
        /// <returns>The environment variable tuple.</returns>
        public abstract Tuple<string, string> GetEnvironmentVariable();

        /// <summary>
        /// Determines if <paramref name="url"/> is a valid Git url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>True if valid.</returns>
        protected static bool IsValidGitUrl(string url)
        {
            string regex = @"((^https:\/\/github.com\/([a-zA-Z0-9-_~.\/])+)((.git)*|((tree|pull|commit|releases\/tag)([a-zA-Z0-9-_~.]+)+)))$";
            return Regex.IsMatch(url, regex);
        }
    }

    /// <summary>
    /// Generate the git repo url environment variable.
    /// </summary>
    public class EnvVarGitRepoUrl : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitRepoUrl"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitRepoUrl(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null
                && CloudEnvironment.Seed.SeedType == SeedType.Git
                && IsValidGitUrl(CloudEnvironment.Seed.SeedMoniker))
            {
                var moniker = CloudEnvironment.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    return new Tuple<string, string>(EnvironmentVariableConstants.GitRepoUrl, repoUrl[0]);
                }
                else
                {
                    return new Tuple<string, string>(EnvironmentVariableConstants.GitRepoUrl, moniker);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the git pull PR number environment variable.
    /// </summary>
    public class EnvVarGitPullPRNumber : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitPullPRNumber"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitPullPRNumber(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null
                && CloudEnvironment.Seed.SeedType == SeedType.Git
                && IsValidGitUrl(CloudEnvironment.Seed.SeedMoniker))
            {
                var moniker = CloudEnvironment.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    return new Tuple<string, string>(EnvironmentVariableConstants.GitPRNumber, Regex.Match(repoUrl[1], "(\\d+)").ToString());
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the git config username.
    /// </summary>
    public class EnvVarGitConfigUserName : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitConfigUserName"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitConfigUserName(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null && CloudEnvironment.Seed.GitConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(CloudEnvironment.Seed.GitConfig.UserName))
                {
                    return new Tuple<string, string>(EnvironmentVariableConstants.GitConfigUsername, CloudEnvironment.Seed.GitConfig.UserName);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the git onfig user email environment variable.
    /// </summary>
    public class EnvVarGitConfigUserEmail : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitConfigUserEmail"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitConfigUserEmail(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null && CloudEnvironment.Seed.GitConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(CloudEnvironment.Seed.GitConfig.UserEmail))
                {
                    return new Tuple<string, string>(EnvironmentVariableConstants.GitConfigUserEmail, CloudEnvironment.Seed.GitConfig.UserEmail);
                }
            }

            return null;
        }
    }

    /// <summary>
    ///  Session settings (typically from AppSettings).
    /// </summary>
    public class SessionSettings
    {
        /// <summary>
        /// Gets or sets the preferred schema, http or https.
        /// </summary>
        public string PreferredSchema { get; set; }

        /// <summary>
        /// Gets or sets the default host name.
        /// </summary>
        public string DefaultHost { get; set; }

        /// <summary>
        /// Gets or sets the default path.
        /// </summary>
        public string DefaultPath { get; set; }
    }

    /// <summary>
    /// Generate the session callbackurl from the session settings.
    /// </summary>
    public class EnvVarSessionCallback : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionCallback"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="sessionSettings">The session settings.</param
        public EnvVarSessionCallback(CloudEnvironment cloudEnvironment, SessionSettings sessionSettings)
            : base(cloudEnvironment)
        {
            SessionSettings = sessionSettings;
        }

        private SessionSettings SessionSettings { get; }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            string apiUrl = SessionSettings.PreferredSchema + "://" + SessionSettings.DefaultHost + SessionSettings.DefaultPath + "/registration/";
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionCallback, apiUrl + CloudEnvironment.Id + "/_callback");
        }
    }

    /// <summary>
    /// Generate the session access token.
    /// </summary>
    public class EnvVarSessionToken : EnvironmentVariableStrategy
    {
        private readonly string accessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionToken"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public EnvVarSessionToken(string accessToken)
            : base(null) => this.accessToken = accessToken;

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionToken, this.accessToken);
        }
    }

    /// <summary>
    /// Generate the sesison id environment variable.
    /// </summary>
    public class EnvVarSessionId : EnvironmentVariableStrategy
    {
        private readonly string sessionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionId"/> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        public EnvVarSessionId(string sessionId)
            : base(null) => this.sessionId = sessionId;

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionId, this.sessionId);
        }
    }

    /// <summary>
    /// Generate the dot-files repo url environment variable.
    /// </summary>
    public class EnvDotfilesRepoUrl : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvDotfilesRepoUrl"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvDotfilesRepoUrl(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Personalization != null
                && !string.IsNullOrWhiteSpace(CloudEnvironment.Personalization.DotfilesRepository)
                && IsValidGitUrl(CloudEnvironment.Personalization.DotfilesRepository))
            {
                return new Tuple<string, string>(
                    EnvironmentVariableConstants.DotfilesRepository,
                    CloudEnvironment.Personalization.DotfilesRepository);
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the dot-files target path environment variable.
    /// </summary>
    public class EnvDotfilesTargetPath : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvDotfilesTargetPath"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvDotfilesTargetPath(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Personalization != null
                && !string.IsNullOrWhiteSpace(CloudEnvironment.Personalization.DotfilesTargetPath))
            {
                return new Tuple<string, string>(
                    EnvironmentVariableConstants.DotfilesTargetPath,
                    CloudEnvironment.Personalization.DotfilesTargetPath);
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the dot-files install command environment variable.
    /// </summary>
    public class EnvDotfilesInstallCommand : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvDotfilesInstallCommand"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvDotfilesInstallCommand(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Personalization != null
                && !string.IsNullOrWhiteSpace(CloudEnvironment.Personalization.DotfilesInstallCommand))
            {
                return new Tuple<string, string>(
                    EnvironmentVariableConstants.DotfilesInstallCommand,
                    CloudEnvironment.Personalization.DotfilesInstallCommand);
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the default shell environment variable.
    /// </summary>
    public class EnvDefaultShell : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvDefaultShell"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvDefaultShell(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Personalization != null
                && !string.IsNullOrWhiteSpace(CloudEnvironment.Personalization.DefaultShell)
                && IsSupportedShell(CloudEnvironment.Personalization.DefaultShell))
            {
                return new Tuple<string, string>(
                    EnvironmentVariableConstants.DefaultShell,
                    CloudEnvironment.Personalization.DefaultShell);
            }

            return null;
        }

        private bool IsSupportedShell(string shell)
        {
            var availableShells = new string[]
            {
                "/bin/bash",
                "/usr/bin/fish",
                "/usr/bin/zsh",
            };

            // The value for this is programatically filled so doesn't need normalization.
            return availableShells.Contains(shell);
        }
    }
}
