// <copyright file="ContactDataInfoHolder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Serialize/Deserialize using in-memory members.
    /// </summary>
    public class ContactDataInfoHolder : ContactDataInfoHolderBase
    {
        private ContactDataInfo localDataInfo;
        private ContactDataInfo remoteDataInfo;

        public ContactDataInfoHolder()
        {
            this.localDataInfo = new Dictionary<string, IDictionary<string, ConnectionProperties>>();
        }

        protected override ContactDataInfo ReadLocalDataInfo() => this.localDataInfo;

        protected override void WriteLocalDataInfo(ContactDataInfo localDataInfo)
        {
            this.localDataInfo = localDataInfo;
        }

        protected override ContactDataInfo ReadRemoteDataInfo() => this.remoteDataInfo;

        protected override void WriteRemoteDataInfo(ContactDataInfo remoteDataInfo)
        {
            this.remoteDataInfo = remoteDataInfo;
        }
    }
}
