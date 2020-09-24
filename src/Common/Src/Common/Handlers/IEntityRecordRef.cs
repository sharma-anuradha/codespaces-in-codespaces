// <copyright file="IEntityRecordRef.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers
{
    public interface IEntityRecordRef<T>
    {
        public T Value { get; set; }
    }
}
