using Bond;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// The experimental features body.
    /// </summary>
    public class ExperimentalFeaturesBody
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentalFeaturesBody"/> class.
        /// </summary>
        public ExperimentalFeaturesBody()
        {
            CustomContainers = false;
            NewTerminal = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use custom containers for this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool CustomContainers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the new terminal output for this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool NewTerminal { get; set; }
    }
}
