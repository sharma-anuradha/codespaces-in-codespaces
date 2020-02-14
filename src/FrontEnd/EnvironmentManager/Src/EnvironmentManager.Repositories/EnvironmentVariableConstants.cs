// <copyright file="EnvironmentVariableConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories
{
    /// <summary>
    /// Environment variable constants.
    /// </summary>
    public static class EnvironmentVariableConstants
    {
#pragma warning disable SA1600 // Elements should be documented
        public const string EnvironmentId = "CLOUDENV_ENVIRONMENT_ID";
        public const string ServiceEndpoint = "CLOUDENV_SERVICE_ENDPOINT";
        public const string GitRepoUrl = "GIT_REPO_URL";
        public const string GitRepoCommit = "GIT_REPO_COMMIT";
        public const string GitPRNumber = "GIT_PR_NUM";
        public const string GitConfigUsername = "GIT_CONFIG_USER_NAME";
        public const string GitConfigUserEmail = "GIT_CONFIG_USER_EMAIL";
        public const string SessionCallback = "SESSION_CALLBACK";
        public const string SessionToken = "SESSION_TOKEN";
        public const string SessionCascadeToken = "SESSION_CASCADE_TOKEN";
        public const string LiveShareServiceUrl = "LIVESHARE_SERVICE_URL";
        public const string SessionId = "SESSION_ID";
        public const string DotfilesRepository = "DOTFILES_REPOSITORY";
        public const string DotfilesTargetPath = "DOTFILES_REPOSITORY_TARGET";
        public const string DotfilesInstallCommand = "DOTFILES_INSTALL_COMMAND";
        public const string AutoShutdownTime = "AUTO_SHUTDOWN_TIME";

        public const string FeatureFlagCustomContainers = "FF_CUSTOM_CONTAINERS";
        public const string FeatureFlagNewTerminal = "FF_NEW_TERMINAL";
#pragma warning restore SA1600 // Elements should be documented
    }
}
