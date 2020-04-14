﻿// <copyright file="SequenceRelayDataHubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Class to buffer a sequence on a IRelayDataHubProxy interface.
    /// </summary>
    public class SequenceRelayDataHubProxy : RelayDataHubProxy
    {
        private readonly SortedList<int, ReceiveDataEventArgs> receivedDataBuffer = new SortedList<int, ReceiveDataEventArgs>();
        private readonly object receiveDataLock = new object();

        public SequenceRelayDataHubProxy(
            IRelayDataHubProxy relayDataHubProxy,
            Func<ReceiveDataEventArgs, bool> filterEventCallback,
            int? currentSequence = null)
            : base(relayDataHubProxy, filterEventCallback)
        {
            if (currentSequence.HasValue)
            {
                CurrentSequence = currentSequence.Value;
            }
        }

        /// <summary>
        /// Gets the current sequence being tracked.
        /// </summary>
        public int CurrentSequence { get; private set; }

        /// <summary>
        /// Gets the total events tracked so far.
        /// </summary>
        public int TotalEvents { get; private set; }

        /// <inheritdoc/>
        protected override void ProcessReceiveData(ReceiveDataEventArgs e)
        {
            lock (this.receiveDataLock)
            {
                int sequence;
                if (e.MessageProperties == null || (sequence = e.MessageProperties.TryGetProperty(RelayHubMessageProperties.PropertySequenceId, -1)) == -1)
                {
                    // no message property found
                    FireReceiveData(e);
                    return;
                }

                if (CurrentSequence == -1 || (CurrentSequence + 1) == sequence)
                {
                    // no need to buffer
                    CurrentSequence = sequence;
                    FireReceiveData(e);
                    while (TryRemoveValue(CurrentSequence + 1, out e))
                    {
                        FireReceiveData(e);
                        ++CurrentSequence;
                    }
                }
                else
                {
#if DEBUG
                    if (sequence > CurrentSequence && (sequence - CurrentSequence) > 15)
                    {
                        Debug.WriteLine($"###-----> sequence/current:{sequence}/{CurrentSequence}");
                    }
#endif
                    ++TotalEvents;

                    // out of order sequence
                    this.receivedDataBuffer.Add(sequence, e);
                }
            }
        }

        private bool TryRemoveValue(int sequence, out ReceiveDataEventArgs e)
        {
            if (this.receivedDataBuffer.TryGetValue(sequence, out e))
            {
                this.receivedDataBuffer.Remove(sequence);
                return true;
            }

            return false;
        }
    }
}
