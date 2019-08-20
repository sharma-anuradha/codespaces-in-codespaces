// <copyright file="AzureDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// This represents virtual machine resource states.
    /// </summary>
    internal struct VmResourceState
    {
        /// <summary>
        /// Gets/Sets the resource name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets/Sets the resource state.
        /// </summary>
        public DeploymentState State;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmResourceState"/> struct.
        /// </summary>
        /// <param name="name">name of the resource.</param>
        /// <param name="state">state of the resource.</param>
        public VmResourceState(string name, DeploymentState state)
        {
            Name = name;
            State = state;
        }

        /// <summary>
        /// Convert tuple to VmResourceState.
        /// </summary>
        /// <param name="value">Tuple that contains the input.</param>
        public static implicit operator (string, DeploymentState)(VmResourceState value)
        {
            return (value.Name, value.State);
        }

        public static implicit operator VmResourceState((string, DeploymentState) value)
        {
            return new VmResourceState(value.Item1, value.Item2);
        }
    }
}