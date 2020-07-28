// <copyright file="MessageColorConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.PrivatePreview
{
    /// <summary>
    /// Console color constants for each type of output message.
    /// </summary>
    public static class MessageColorConstants
    {
#pragma warning disable SA1600 // Elements should be documented
        public const ConsoleColor Add = ConsoleColor.Green;
        public const ConsoleColor Delete = ConsoleColor.Green;
        public const ConsoleColor Update = ConsoleColor.Yellow;
        public const ConsoleColor Skip = ConsoleColor.White;
#pragma warning restore SA1600 // Elements should be documented
    }
}
