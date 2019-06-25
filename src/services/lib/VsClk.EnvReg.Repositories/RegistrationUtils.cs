using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VsSaaS.Diagnostics;

namespace VsClk.EnvReg.Repositories
{

    class RegistrationUtils
    {

        private static readonly Dictionary<string, string> stampRegionMap = new Dictionary<string, string>()
        {
            {"use", "eastus"},
            {"usw2", "westus2"},
            {"euw", "westeurope"},
            {"asse", "southeastasia"},
        };

        /// <summary>
        /// Convert the provided stamp to a full Azure region name.
        /// If it is already a full region name, the value is returned unchanged.
        /// e.g.!-- use -> eastus
        /// e.g.!-- eastus -> eastus
        /// e.g.!-- null -> null
        /// </summary>
        /// <param name="stampLocation">The stamp name</param>
        /// <returns></returns>
        public static string StampToRegion(string stampLocation)
        {
            if (stampLocation == null)
            {
                return null;
            }
            if (stampRegionMap.ContainsValue(stampLocation))
            {
                return stampLocation;
            }
            return stampRegionMap.GetValueOrDefault(stampLocation);
        }

    }
}
