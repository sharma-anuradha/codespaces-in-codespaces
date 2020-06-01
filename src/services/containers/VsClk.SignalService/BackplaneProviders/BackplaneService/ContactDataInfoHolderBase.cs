// <copyright file="ContactDataInfoHolderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Thread safe instance to hold a ContactDataInfo structure.
    /// </summary>
    public abstract class ContactDataInfoHolderBase
    {
        private readonly object lockDataInfo = new object();

        public int ConnectionsCount
        {
            get
            {
                lock (this.lockDataInfo)
                {
                    return ReadLocalDataInfo().GetConnectionsCount();
                }
            }
        }

        public ContactDataInfo GetAggregatedDataInfo()
        {
            lock (this.lockDataInfo)
            {
                var dataInfo = ReadLocalDataInfo().Clone();
                var remoteDataInfo = ReadRemoteDataInfo();
                if (remoteDataInfo != null)
                {
                    dataInfo = dataInfo.Concat(remoteDataInfo.Clone()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                return dataInfo;
            }
        }

        public void UpdateRemote(ContactDataInfo remoteDataInfo)
        {
            lock (this.lockDataInfo)
            {
                var localDataInfo = ReadLocalDataInfo();
                remoteDataInfo = remoteDataInfo.Clone();
                RemoveRemoteLocalServices(localDataInfo, remoteDataInfo);
                remoteDataInfo.CleanupServiceConnections();
                WriteRemoteDataInfo(remoteDataInfo);
            }
        }

        public void UpdateLocal(ContactDataInfo localDataChanged, string[] activeServices)
        {
            UpdateLocal(
                localDataInfo =>
                {
                    // merge this local changes with existing ones.
                    foreach (var kvp in localDataChanged)
                    {
                        localDataInfo[kvp.Key] = kvp.Value;
                    }
                },
                activeServices);
        }

        public void UpdateLocal(ContactDataChanged<ConnectionProperties> localDataChanged, string[] activeServices)
        {
            UpdateLocal(
                localDataInfo =>
                {
                    // merge the connection properties.
                    localDataInfo.UpdateConnectionProperties(localDataChanged);
                },
                activeServices);
        }

        protected abstract ContactDataInfo ReadLocalDataInfo();

        protected abstract void WriteLocalDataInfo(ContactDataInfo localDataInfo);

        protected abstract ContactDataInfo ReadRemoteDataInfo();

        protected abstract void WriteRemoteDataInfo(ContactDataInfo remoteDataInfo);

        private static void UpdateLocalServices(
            ContactDataInfo localDataInfo,
            ContactDataInfo remoteDataInfo,
            string[] activeServices)
        {
            Assumes.NotNull(localDataInfo);
            RemoveRemoteLocalServices(localDataInfo, remoteDataInfo);

            // Note: next block will remove 'stale' service entries
            foreach (var serviceId in localDataInfo.Keys.Where(serviceId => !activeServices.Contains(serviceId)).ToArray())
            {
                localDataInfo.Remove(serviceId);
            }

            localDataInfo.CleanupServiceConnections();
            remoteDataInfo?.CleanupServiceConnections();
        }

        private static void RemoveRemoteLocalServices(
            ContactDataInfo localDataInfo,
            ContactDataInfo remoteDataInfo)
        {
            Assumes.NotNull(localDataInfo);

            // Note: next block will remove local services from our remote data info to avoid duplication
            if (remoteDataInfo != null)
            {
                foreach (var serviceId in localDataInfo.Keys)
                {
                    remoteDataInfo.Remove(serviceId);
                }
            }
        }

        private void UpdateLocal(Action<ContactDataInfo> callback, string[] activeServices)
        {
            lock (this.lockDataInfo)
            {
                var localDataInfo = ReadLocalDataInfo();
                var remoteDataInfo = ReadRemoteDataInfo();

                callback(localDataInfo);

                UpdateLocalServices(localDataInfo, remoteDataInfo, activeServices);

                WriteLocalDataInfo(localDataInfo);
                WriteRemoteDataInfo(remoteDataInfo);
            }
        }
    }
}
