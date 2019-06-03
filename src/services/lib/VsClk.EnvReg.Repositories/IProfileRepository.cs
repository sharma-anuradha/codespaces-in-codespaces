using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using VsClk.EnvReg.Models.DataStore;

namespace VsClk.EnvReg.Repositories
{
    public interface IProfileRepository
    {
        /// <summary>
        /// Get the user profile for the current users
        /// </summary>
        /// <param name="logger">Logger to be used</param>
        /// <returns>A collection of inactive profiles.</returns>
        Task<Profile> GetCurrentUserProfileAsync(IDiagnosticsLogger logger);
    }
}
