// <copyright file="AzureLocation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Specifies the Azure location name.
    /// Naming and casing matches the Azure definitions.
    /// </summary>
    public enum AzureLocation
    {
        /// <summary>
        /// Australia Central
        /// </summary>
        AustraliaCentral = AzureGeos.Australia + 1,

        /// <summary>
        /// Australia Central 2
        /// </summary>
        AustraliaCentral2 = AzureGeos.Australia + 2,

        /// <summary>
        /// Australia East
        /// </summary>
        AustraliaEast = AzureGeos.Australia + 3,

        /// <summary>
        /// Australia SouthEast
        /// </summary>
        AustraliaSouthEast = AustraliaCentral + 4,

        /// <summary>
        /// Brazil South,
        /// </summary>
        BrazilSouth = AzureGeos.Brasil + 1,

        /// <summary>
        /// Canada Central
        /// </summary>
        CanadaCentral = AzureGeos.Canada + 1,

        /// <summary>
        /// Canada East
        /// </summary>
        CanadaEast = AzureGeos.Canada + 2,

        /// <summary>
        /// Central US
        /// </summary>
        CentralUs = AzureGeos.Us + 1,

        /// <summary>
        /// /// Central India
        /// </summary>
        CentralIndia = AzureGeos.India + 1,

        /// <summary>
        /// East Asia
        /// </summary>
        EastAsia = AzureGeos.Asia + 1,

        /// <summary>
        /// East US
        /// </summary>
        EastUs = AzureGeos.Us + 2,

        /// <summary>
        /// East US 2
        /// </summary>
        EastUs2 = AzureGeos.Us + 3,

        /// <summary>
        /// France Central
        /// </summary>
        FranceCentral = AzureGeos.France + 1,

        /// <summary>
        /// France South
        /// </summary>
        FranceSouth = AzureGeos.France + 2,

        /// <summary>
        /// Japan East
        /// </summary>
        JapanEast = AzureGeos.Japan + 1,

        /// <summary>
        /// Japan West
        /// </summary>
        JapanWest = AzureGeos.Japan + 2,

        /// <summary>
        /// Korea Central
        /// </summary>
        KoreaCentral = AzureGeos.Korea + 1,

        /// <summary>
        /// Korea South
        /// </summary>
        KoreaSouth = AzureGeos.Korea + 2,

        /// <summary>
        /// North Central US
        /// </summary>
        NorthCentralUs = AzureGeos.Us + 4,

        /// <summary>
        /// North Europe
        /// </summary>
        NorthEurope = AzureGeos.Europe + 1,

        /// <summary>
        /// South Africa North
        /// </summary>
        SouthAfricaNorth = AzureGeos.SouthAfrica + 1,

        /// <summary>
        /// South Africa West
        /// </summary>
        SouthAfricaWest = AzureGeos.SouthAfrica + 2,

        /// <summary>
        /// South Central US
        /// </summary>
        SouthCentralUs = AzureGeos.Us + 5,

        /// <summary>
        /// SouthEast Asia
        /// </summary>
        SouthEastAsia = AzureGeos.Asia + 2,

        /// <summary>
        /// South India
        /// </summary>
        SouthIndia = AzureGeos.India + 2,

        /// <summary>
        /// UAE Central
        /// </summary>
        UaeCentral = AzureGeos.Uae + 1,

        /// <summary>
        /// UAE North
        /// </summary>
        UaeNorth = AzureGeos.Uae + 2,

        /// <summary>
        /// UK South
        /// </summary>
        UkSouth = AzureGeos.Uk + 1,

        /// <summary>
        /// UK West
        /// </summary>
        UkWest = AzureGeos.Uk + 2,

        /// <summary>
        /// West Central US
        /// </summary>
        WestCentralUs = AzureGeos.Us + 6,

        /// <summary>
        /// West Europe
        /// </summary>
        WestEurope = AzureGeos.Europe + 2,

        /// <summary>
        /// West India
        /// </summary>
        WestIndia = AzureGeos.India + 3,

        /// <summary>
        /// West US
        /// </summary>
        WestUs = AzureGeos.Us + 7,

        /// <summary>
        /// West US 2
        /// </summary>
        WestUs2 = AzureGeos.Us + 8,
    }

#pragma warning disable SA1602 // Enumeration items should be documented
    /// <summary>
    /// Used for initializing <see cref="AzureLocation"/> but not exposed publicly.
    /// </summary>
    internal enum AzureGeos
    {
        Asia = 100,
        Australia = 200,
        Brasil = 300,
        Canada = 400,
        Europe = 500,
        France = 600,
        India = 700,
        Japan = 800,
        Korea = 900,
        SouthAfrica = 1000,
        Uae = 1200,
        Unused = 1300, // skipping 13 :)
        Uk = 1400, 
        Us = 1500, 
    }
#pragma warning restore SA1602 // Enumeration items should be documented

}
