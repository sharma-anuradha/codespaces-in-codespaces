// <copyright file="ValidationError.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Token service validation error codes.
    /// </summary>
    public enum ValidationError
    {
        None = 0,
        Unknown,
        SignatureKeyNotFound,
        InvalidSignature,
        InvalidIssuer,
        InvalidAudience,
        DecryptionFailed,
        Expired,
    }
}
