// <copyright file="ProfileExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        public const string VisualStudioOnlineWidowsSkuPreviewUserProgram = "vsonline.windowsskupreview";

        /// <summary>
        /// Test whether the given user profile is a member of the Windows SKU preview.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>True if the test succeeded.</returns>
        public static bool IsWindowsSkuPreviewUser(this Profile profile)
        {
            return profile.GetProgramsItem<bool>(VisualStudioOnlineWidowsSkuPreviewUserProgram)
                || (profile.Email?.EndsWith("@microsoft.com") ?? false);
        }

        /// <summary>
        /// Test whether the given user profile is an internal AAD member.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>True if the test succeeded.</returns>
        public static bool IsWindowsSkuInternalUser(this Profile profile)
        {
            return profile.Email?.EndsWith("@microsoft.com") ?? false;
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
