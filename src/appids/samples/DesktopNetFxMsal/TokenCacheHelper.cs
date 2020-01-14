﻿// <copyright file="TokenCacheHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace DesktopNetFxMsal
{
    /// <summary>
    /// MSAL token cache helper.
    /// </summary>
    public static class TokenCacheHelper
    {
        /// <summary>
        /// Path to the token cache.
        /// </summary>
        private static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin";

        private static readonly object FileLock = new object();

        /// <summary>
        /// Enable serialization for the given token cache.
        /// </summary>
        /// <param name="tokenCache">The token cache.</param>
        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(
                    File.Exists(CacheFilePath) ?
                    ProtectedData.Unprotect(
                        File.ReadAllBytes(CacheFilePath),
                        null,
                        DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(
                        CacheFilePath,
                        ProtectedData.Protect(
                            args.TokenCache.SerializeMsalV3(),
                            null,
                            DataProtectionScope.CurrentUser));
                }
            }
        }
    }
}
