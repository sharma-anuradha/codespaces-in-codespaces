// <copyright file="EnvironmentVariableGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
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
        /// <param name="serviceUri">The service uri.</param>
        /// <param name="callbackUri">The callback uri.</param>
        /// <param name="cascadeToken">The cascade token for the environment.</param>
        /// <param name="cloudEnvironmentOptions">The cloud environment options.</param>
        /// <returns>A dictionary of environment variables.</returns>
        public static Dictionary<string, string> Generate(
            CloudEnvironment cloudEnvironment,
            Uri serviceUri,
            Uri callbackUri,
            string cascadeToken,
            CloudEnvironmentOptions cloudEnvironmentOptions)
        {
            var result = new Dictionary<string, string>();

            if (cloudEnvironment.Features != null)
            {
                foreach (var keyValuePair in cloudEnvironment.Features)
                {
                    var featureKey = $"FF_{keyValuePair.Key.ToUpperInvariant()}";
                    result.Add(featureKey, keyValuePair.Value);
                }
            }

            var list = new EnvironmentVariableStrategy[]
            {
                // Variables for vscode cloudenv extension
                new EnvVarEnvironmentId(cloudEnvironment),
                new EnvVarServiceEndpoint(cloudEnvironment, serviceUri),
                new EnvVarAutoShutdownTime(cloudEnvironment),

                // Variables for repository seed
                new EnvVarGitRepoUrl(cloudEnvironment),
                new EnvVarGitSubmoduleClone(cloudEnvironment),
                new EnvVarGitRepoCommit(cloudEnvironment),
                new EnvVarGitConfigUserName(cloudEnvironment),
                new EnvVarGitConfigUserEmail(cloudEnvironment),

                // Variables for session bootstrap
                new EnvVarSessionCallback(cloudEnvironment, callbackUri),
                new EnvVarSessionCascadeToken(cascadeToken),
                new EnvVarSessionId(cloudEnvironment),
                new EnvVarLiveshareServiceEndpoint(cloudEnvironment, cloudEnvironment.Connection.ConnectionServiceUri),

                // Variables for personalization
                new EnvDotfilesRepoUrl(cloudEnvironment),
                new EnvDotfilesTargetPath(cloudEnvironment),
                new EnvDotfilesInstallCommand(cloudEnvironment),
            };

            foreach (var envStrategy in list)
            {
                var envVar = new EnvironmentVariableContext(envStrategy).ExecuteStrategy();
                if (envVar != null)
                {
                    result.Add(envVar.Item1, envVar.Item2);
                }
            }

            if (cloudEnvironmentOptions != null)
            {
                var optionsList = new EnvironmentFeatureFlagsStrategy[]
                {
                    new FeatureCustomContainers(cloudEnvironmentOptions),
                    new FeatureNewTerminal(cloudEnvironmentOptions),
                    new FeatureShallowClone(cloudEnvironmentOptions),
                };

                foreach (var flag in optionsList)
                {
                    var flagValues = flag.GetEnvironmentVariable();
                    if (flagValues != null)
                    {
                        result.Add(flagValues.Item1, flagValues.Item2);
                    }
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
        private static readonly List<string> Schemas = new List<string> { "http", "https" };

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
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (Schemas.Contains(uri.Scheme))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Generates the environment auto shutdown time.
    /// </summary>
    public class EnvVarAutoShutdownTime : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarAutoShutdownTime"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarAutoShutdownTime(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.AutoShutdownTime, CloudEnvironment.AutoShutdownDelayMinutes.ToString());
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

                return new Tuple<string, string>(EnvironmentVariableConstants.GitRepoUrl, moniker);
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the git submodule clone variable.
    /// </summary>
    public class EnvVarGitSubmoduleClone : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitSubmoduleClone"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitSubmoduleClone(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null)
            {
                bool recurseClone = CloudEnvironment.Seed.RecurseClone;
                return new Tuple<string, string>(EnvironmentVariableConstants.GitEnableSubmoduleClone, recurseClone.ToString());
            }

            return null;
        }
    }

    /// <summary>
    /// Generate the environment id variable.
    /// </summary>
    public class EnvVarEnvironmentId : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarEnvironmentId"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarEnvironmentId(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.EnvironmentId, CloudEnvironment.Id);
        }
    }

    /// <summary>
    /// Generate the service endpoint environment variable.
    /// </summary>
    public class EnvVarServiceEndpoint : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarServiceEndpoint"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="serviceUri">The service Uri.</param>
        public EnvVarServiceEndpoint(CloudEnvironment cloudEnvironment, Uri serviceUri)
            : base(cloudEnvironment)
        {
            Requires.NotNull(serviceUri, nameof(serviceUri));

            ServiceUri = serviceUri;
        }

        private Uri ServiceUri { get; }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.ServiceEndpoint, ServiceUri.ToString());
        }
    }

    /// <summary>
    /// Generate the liveshare service url.
    /// </summary>
    public class EnvVarLiveshareServiceEndpoint : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarLiveshareServiceEndpoint"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="serviceUrl">The service uri.</param>
        public EnvVarLiveshareServiceEndpoint(CloudEnvironment cloudEnvironment, string serviceUrl)
            : base(cloudEnvironment)
        {
            if (cloudEnvironment.State != CloudEnvironmentState.Exporting)
            {
                Requires.NotNull(serviceUrl, nameof(serviceUrl));

                var serviceUri = new Uri(serviceUrl);

                ServiceEndpoint = serviceUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
            }
            else
            {
                ServiceEndpoint = null;
            }
        }

        private string ServiceEndpoint { get; }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            // Service endpoint is null only if user is exporting as assigned in the constructor
            if (ServiceEndpoint == null)
            {
                return null;
            }

            return new Tuple<string, string>(EnvironmentVariableConstants.LiveShareServiceUrl, ServiceEndpoint);
        }
    }

    /// <summary>
    /// Generate the git pull PR number environment variable.
    /// </summary>
    public class EnvVarGitRepoCommit : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarGitRepoCommit"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarGitRepoCommit(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            if (CloudEnvironment.Seed != null
                && CloudEnvironment.Seed.SeedType == SeedType.Git
                && !string.IsNullOrEmpty(CloudEnvironment.Seed.SeedCommit))
            {
                var checkout = CloudEnvironment.Seed.SeedCommit;

                return new Tuple<string, string>(EnvironmentVariableConstants.GitRepoCommit, checkout);
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
    /// Generate the git config user email environment variable.
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
    /// Generate the session callbackurl from the session settings.
    /// </summary>
    public class EnvVarSessionCallback : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionCallback"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="callbackUri">The callback uri.</param
        public EnvVarSessionCallback(CloudEnvironment cloudEnvironment, Uri callbackUri)
            : base(cloudEnvironment)
        {
            CallbackUri = callbackUri;
        }

        private Uri CallbackUri { get; }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionCallback, CallbackUri.AbsoluteUri);
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
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionToken, accessToken);
        }
    }

    /// <summary>
    /// Generate the session cascade token.
    /// </summary>
    public class EnvVarSessionCascadeToken : EnvironmentVariableStrategy
    {
        private readonly string cascadeToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionCascadeToken"/> class.
        /// </summary>
        /// <param name="cascadeToken">The access token.</param>
        public EnvVarSessionCascadeToken(string cascadeToken)
            : base(null) => this.cascadeToken = cascadeToken;

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionCascadeToken, cascadeToken);
        }
    }

    /// <summary>
    /// Generate the sesison id environment variable.
    /// </summary>
    public class EnvVarSessionId : EnvironmentVariableStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvVarSessionId"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        public EnvVarSessionId(CloudEnvironment cloudEnvironment)
            : base(cloudEnvironment)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(EnvironmentVariableConstants.SessionId, CloudEnvironment.Connection.ConnectionSessionId);
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
    /// Environment variables strategy for feature flags.
    /// </summary>
    public abstract class EnvironmentFeatureFlagsStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFeatureFlagsStrategy"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentOptions">The cloud environment options.</param>
        public EnvironmentFeatureFlagsStrategy(CloudEnvironmentOptions cloudEnvironmentOptions)
        {
            CloudEnvironmentOptions = cloudEnvironmentOptions;
        }

        /// <summary>
        /// Gets the cloud environment instance.
        /// </summary>
        public CloudEnvironmentOptions CloudEnvironmentOptions { get; }

        /// <summary>
        /// Get the environment variable for this strategy.
        /// </summary>
        /// <returns>The environment variable tuple.</returns>
        public abstract Tuple<string, string> GetEnvironmentVariable();
    }

    /// <summary>
    /// Feature flag for custom containers functionality.
    /// </summary>
    public class FeatureCustomContainers : EnvironmentFeatureFlagsStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureCustomContainers"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentOptions">THe cloud environment options.</param>
        public FeatureCustomContainers(CloudEnvironmentOptions cloudEnvironmentOptions)
            : base(cloudEnvironmentOptions)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(
                    EnvironmentVariableConstants.FeatureFlagCustomContainers,
                    CloudEnvironmentOptions.CustomContainers.ToString());
        }
    }

    /// <summary>
    /// Feature flag for using the new terminal functionality.
    /// </summary>
    public class FeatureNewTerminal : EnvironmentFeatureFlagsStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureNewTerminal"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentOptions">The cloud environment options.</param>
        public FeatureNewTerminal(CloudEnvironmentOptions cloudEnvironmentOptions)
            : base(cloudEnvironmentOptions)
        {
        }

        /// <inheritdoc/>
        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(
                    EnvironmentVariableConstants.FeatureFlagNewTerminal,
                    CloudEnvironmentOptions.NewTerminal.ToString());
        }
    }

    public class FeatureShallowClone : EnvironmentFeatureFlagsStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureShallowClone"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentOptions">The cloud environment options.</param>
        public FeatureShallowClone(CloudEnvironmentOptions cloudEnvironmentOptions)
            : base(cloudEnvironmentOptions)
        {
        }

        public override Tuple<string, string> GetEnvironmentVariable()
        {
            return new Tuple<string, string>(
                    EnvironmentVariableConstants.FeatureFlagShallowClone,
                    CloudEnvironmentOptions.ShallowClone.ToString());
        }
    }
}
