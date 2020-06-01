// <copyright file="MessagePackDataInfoHolder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Serialize/Deserialize using in-memory members.
    /// </summary>
    public class MessagePackDataInfoHolder : ContactDataInfoHolderBase
    {
        private MessagePackDataBuffer<ContactDataInfo> localDataInfoBuffer = new MessagePackDataBuffer<ContactDataInfo>(new Dictionary<string, IDictionary<string, ConnectionProperties>>());
        private MessagePackDataBuffer<ContactDataInfo> remoteDataInfoBuffer = new MessagePackDataBuffer<ContactDataInfo>();

        protected override ContactDataInfo ReadLocalDataInfo() => this.localDataInfoBuffer.Data;

        protected override void WriteLocalDataInfo(ContactDataInfo localDataInfo)
        {
            this.localDataInfoBuffer.Data = localDataInfo;
        }

        protected override ContactDataInfo ReadRemoteDataInfo() => this.remoteDataInfoBuffer.Data;

        protected override void WriteRemoteDataInfo(ContactDataInfo remoteDataInfo)
        {
            this.remoteDataInfoBuffer.Data = remoteDataInfo;
        }
    }
}
