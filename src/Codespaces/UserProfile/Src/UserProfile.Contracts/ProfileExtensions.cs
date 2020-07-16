// <copyright file="ProfileExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// <see cref="Profile"/> extensions.
    /// </summary>
    public static class ProfileExtensions
    {
        /// <summary>
        /// The Cloud Environments Preview program name.
        /// </summary>
        public const string CloudEnvironmentsPreviewUserProgram = "vs.cloudenvironements.previewuser";

        /// <summary>
        /// The Windows Preview program name.
        /// </summary>
        public const string VisualStudioOnlineWindowsSkuPreviewUserProgram = "vsonline.windowsskupreview";

        /// <summary>
        /// The Internal Windows SKU program name.
        /// </summary>
        public const string VisualStudioOnlineInternalWindowsSkuUserProgram = "vsonline.internalwindowssku";

        /// <summary>
        /// Test whether the given user profile is a member of the Windows SKU preview.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>True if the test succeeded.</returns>
        public static bool IsWindowsSkuPreviewUser(this Profile profile)
        {
            return profile.GetProgramsItem<bool>(VisualStudioOnlineWindowsSkuPreviewUserProgram)
                || (profile.Email?.EndsWith("@microsoft.com") ?? false);
        }

        /// <summary>
        /// Test whether the given user profile has access to internal-only Windows SKUs.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>True if the test succeeded.</returns>
        public static bool IsWindowsSkuInternalUser(this Profile profile)
        {
            // Check feature flag first
            var featureFlagEnabled = profile.GetProgramsItem<bool>(VisualStudioOnlineInternalWindowsSkuUserProgram);
            if (featureFlagEnabled)
            {
                return true;
            }

            // Fallback to email check if feature flag is not set
            var email = profile.Email;
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            // Find the '@' char and create a string of all characters after it
            var amperstandIndex = email.LastIndexOf('@');
            var emailDomain = email.Substring(amperstandIndex + 1);
            return emailDomain.Equals("microsoft.com", System.StringComparison.OrdinalIgnoreCase) ||
                   emailDomain.EndsWith(".microsoft.com", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test whether the given user profile is a member of the cloud environment preview user program.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>True if the test succeeded.</returns>
        public static bool IsCloudEnvironmentsPreviewUser(this Profile profile)
        {
            return profile.GetProgramsItem<bool>(CloudEnvironmentsPreviewUserProgram)
                || (profile.Email?.EndsWith("@microsoft.com") ?? false);
        }

        /// <summary>
        /// Get the named profile program item, or default if not specified.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="profile">The user profile.</param>
        /// <param name="key">The program item key.</param>
        /// <returns>The value or default.</returns>
        public static T GetProgramsItem<T>(this Profile profile, string key)
        {
            if (profile.Programs == null || !profile.Programs.TryGetValue(key, out var value))
            {
                return default;
            }

            return value is T ? (T)value : default;
        }
    }
}
