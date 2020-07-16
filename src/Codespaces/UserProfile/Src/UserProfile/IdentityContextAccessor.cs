// <copyright file="IdentityContextAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <inheritdoc/>
    public class IdentityContextAccessor : IIdentityContextAccessor
    {
        private static readonly AsyncLocal<IdentityContextHolder> IdentityContextCurrent = new AsyncLocal<IdentityContextHolder>();

        /// <inheritdoc/>
        public IdentityContext IdentityContext
        {
            get
            {
                return IdentityContextCurrent.Value?.Context;
            }

            set
            {
                var holder = IdentityContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current IdentityContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the IdentityContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    IdentityContextCurrent.Value = new IdentityContextHolder { Context = value };
                }
            }
        }

        private class IdentityContextHolder
        {
            public IdentityContext Context { get; set; }
        }
    }
}
