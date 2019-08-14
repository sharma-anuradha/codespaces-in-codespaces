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
        private const string CloudEnvironmentsPreviewUserProgram = "vs.cloudenvironements.previewuser";

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
