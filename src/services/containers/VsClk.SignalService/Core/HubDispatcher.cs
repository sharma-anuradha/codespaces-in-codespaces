using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Class to define a hub dispatcher
    /// </summary>
    public class HubDispatcher
    {
        private Dictionary<string, MethodInfo> methods;

        /// <summary>
        /// Create an instance of a hub dispatcher
        /// </summary>
        /// <param name="hubName">The hub name to dispatch</param>
        /// <param name="hubType">The hub type to dispatch</param>
        public HubDispatcher(string hubName, Type hubType)
        {
            HubName = hubName;
            HubType = hubType;
            this.methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToDictionary(m => m.Name, m => m);
        }

        public string HubName { get; }
        public Type HubType { get; }

        public bool TryGetMethod(string methodName, out MethodInfo methodInfo)
        {
            return this.methods.TryGetValue(methodName, out methodInfo);
        }
    }
}
