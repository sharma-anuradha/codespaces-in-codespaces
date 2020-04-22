// <copyright file="MetricsUtilities.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Utilities for comprehending geographic data.
    /// </summary>
    public static class MetricsUtilities
    {
        // NOTE: this mapping is based on published sovereignty, not necessarily physical geography.
        // See https://en.wikipedia.org/wiki/List_of_ISO_3166_country_codes.
        private static readonly Dictionary<string, AzureGeography> CountryToGeoMap = new Dictionary<string, AzureGeography>
        {
            // Asia Pacific
            { "HK", AzureGeography.AsiaPacific },
            { "SG", AzureGeography.AsiaPacific },

            // Brazil
            { "BR", AzureGeography.Brazil },

            // Canada
            { "CA", AzureGeography.Canada },

            // China
            { "CN", AzureGeography.China },
            { "MO", AzureGeography.China },

            // Europe (EU), except countries with a designated geo
            { "AT", AzureGeography.Europe },
            { "AX", AzureGeography.Europe },
            { "BE", AzureGeography.Europe },
            { "BG", AzureGeography.Europe },
            { "CY", AzureGeography.Europe },
            { "CZ", AzureGeography.Europe },
            { "DK", AzureGeography.Europe },
            { "EE", AzureGeography.Europe },
            { "FI", AzureGeography.Europe },
            { "FO", AzureGeography.Europe },
            { "GL", AzureGeography.Europe },
            { "GR", AzureGeography.Europe },
            { "HR", AzureGeography.Europe },
            { "HU", AzureGeography.Europe },
            { "IE", AzureGeography.Europe },
            { "IT", AzureGeography.Europe },
            { "LT", AzureGeography.Europe },
            { "LU", AzureGeography.Europe },
            { "LV", AzureGeography.Europe },
            { "MT", AzureGeography.Europe },
            { "NL", AzureGeography.Europe },
            { "PT", AzureGeography.Europe },
            { "RO", AzureGeography.Europe },
            { "SI", AzureGeography.Europe },
            { "SK", AzureGeography.Europe },

            // France
            { "FR", AzureGeography.France },

            // Germany
            { "DE", AzureGeography.Germany },

            // India
            { "IN", AzureGeography.India },

            // Japan
            { "JP", AzureGeography.Japan },

            // Korea (republic of korea)
            { "KR", AzureGeography.Korea },

            // Mexico
            { "MX", AzureGeography.Mexico },

            // Norway
            { "NO", AzureGeography.Norway },

            // Poland
            { "PL", AzureGeography.Europe },  // Poland is not announced/GA

            // South Africa
            { "ZA", AzureGeography.SouthAfrica },

            // Spain
            { "ES", AzureGeography.Spain },

            // Sweden
            { "SE", AzureGeography.Europe },  // Sweden is not announced/GA

            // Switzerland
            { "CH", AzureGeography.Switzerland },

            // Taiwan
            { "TW", AzureGeography.Unknown }, // Taiwan is not announced/GA

            // United Arab Emerates
            { "AE", AzureGeography.UAE },

            // United Kingdom
            { "AI", AzureGeography.UnitedKingdom },
            { "BM", AzureGeography.UnitedKingdom },
            { "FK", AzureGeography.UnitedKingdom },
            { "GB", AzureGeography.UnitedKingdom },
            { "GI", AzureGeography.UnitedKingdom },
            { "GS", AzureGeography.UnitedKingdom },
            { "IO", AzureGeography.UnitedKingdom },
            { "KY", AzureGeography.UnitedKingdom },
            { "MS", AzureGeography.UnitedKingdom },
            { "PN", AzureGeography.UnitedKingdom },
            { "SH", AzureGeography.UnitedKingdom },
            { "TC", AzureGeography.UnitedKingdom },
            { "VG", AzureGeography.UnitedKingdom },

            // United States
            { "VI", AzureGeography.UnitedStates },
            { "US", AzureGeography.UnitedStates },
            { "UM", AzureGeography.UnitedStates },
            { "PR", AzureGeography.UnitedStates },
            { "MP", AzureGeography.UnitedStates },
            { "GU", AzureGeography.UnitedStates },
        };

        /// <summary>
        /// Map and ISO Country Code to a public Azure geography.
        /// Sovereign-cloud geographies are not mapped.
        /// </summary>
        /// <param name="isoCountryCode">The two-letter ISO country code.</param>
        /// <returns>The corresponding <see cref="AzureGeography"/>.</returns>
        public static AzureGeography? IsoCountryCodeToAzurePublicGeography(string isoCountryCode)
        {
            if (!string.IsNullOrEmpty(isoCountryCode))
            {
                if (CountryToGeoMap.TryGetValue(isoCountryCode, out var azurePublicGeography))
                {
                    return azurePublicGeography;
                }
            }

            return default;
        }

        /// <summary>
        /// Determine the VSO client type based on HTTP user agent header.
        /// </summary>
        /// <param name="userAgent">The user agent header value.</param>
        /// <returns>The <see cref="VsoClientType"/>.</returns>
        public static VsoClientType? UserAgentToVsoClientType(string userAgent)
        {
            if (userAgent?.StartsWith("Visual Studio Client") == true)
            {
                return VsoClientType.VisualStudio;
            }
            else if (userAgent?.StartsWith("axios/") == true)
            {
                return VsoClientType.VisualStudioCode;
            }
            else if (userAgent?.StartsWith("Mozilla/5.0") == true)
            {
                return VsoClientType.WebPortal;
            }
            else
            {
                return default;
            }
        }
    }
}
