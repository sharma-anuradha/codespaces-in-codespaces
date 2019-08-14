using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The front-end environments http contrat.
    /// </summary>
    public interface IEnvironmentsHttpContract
    {
        /// <summary>
        /// List environments by owner.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironmentResult>> ListEnvironmentsByOwnerAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Create a new environment.
        /// </summary>
        /// <param name="createCloudEnvironmentBody">The create environment body.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The newly crated <see cref="CloudEnvironmentResult"/>.</returns>
        Task<CloudEnvironmentResult> CreateEnvironmentAsync(CreateCloudEnvironmentBody createCloudEnvironmentBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Get an existing environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The newly crated <see cref="CloudEnvironmentResult"/>.</returns>
        Task<CloudEnvironmentResult> GetEnvironmentAsync(string environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes an existing environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task DeleteEnvironmentAsync(string environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Update an existing environment callback.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="callbackOptionsBody">The callback info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task<CloudEnvironmentResult> UpdateEnvironmentCallbackAsync(string environmentId, CallbackOptionsBody callbackOptionsBody, IDiagnosticsLogger logger);
    }
}
